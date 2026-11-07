using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamTaskManager.Application.Projects;
using TeamTaskManager.Application.Activities;
using TeamTaskManager.Application.Notifications;
using TeamTaskManager.Application.Users;
using TeamTaskManager.Domain.Entities;
using TeamTaskManager.Domain.Enums;
using TeamTaskManager.Infrastructure.Persistence;

namespace TeamTaskManager.Infrastructure.Projects;

public sealed class ProjectMemberService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IActivityLogService activityLogService,
    INotificationService notificationService) : IProjectMemberService
{
    public async Task<IReadOnlyCollection<ProjectMemberDto>?> GetMembersAsync(
        ProjectAccessContext access,
        int projectId,
        CancellationToken cancellationToken = default)
    {
        if (!await CanManageProjectAsync(access, projectId, cancellationToken))
        {
            return null;
        }

        return await (
            from member in dbContext.ProjectMembers.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on member.UserId equals user.Id
            where member.ProjectId == projectId
            orderby member.IsActive descending, user.LastName, user.FirstName
            select new ProjectMemberDto(
                member.Id,
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email ?? string.Empty,
                member.MemberRole,
                member.JoinedDate,
                member.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserDto>?> GetEligibleUsersAsync(
        ProjectAccessContext access,
        int projectId,
        CancellationToken cancellationToken = default)
    {
        if (!await CanManageProjectAsync(access, projectId, cancellationToken)) return null;

        return await dbContext.Users.AsNoTracking()
            .Where(user => user.IsActive && !dbContext.ProjectMembers.Any(member =>
                member.ProjectId == projectId && member.UserId == user.Id && member.IsActive))
            .OrderBy(user => user.LastName).ThenBy(user => user.FirstName)
            .Select(user => new UserDto(user.Id, user.FirstName, user.LastName, user.Email ?? string.Empty,
                user.IsActive, user.CreatedDate,
                dbContext.UserRoles.Where(userRole => userRole.UserId == user.Id)
                    .Join(dbContext.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name!)
                    .ToArray()))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectOperationResult<ProjectMemberDto>> AddAsync(
        ProjectAccessContext access,
        int projectId,
        AddProjectMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        var project = await GetManageableProjectAsync(access, projectId, cancellationToken);
        if (project is null)
        {
            return ProjectOperationResult<ProjectMemberDto>.Failure("Proje bulunamadı veya üye yönetme yetkiniz yok.");
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return ProjectOperationResult<ProjectMemberDto>.Failure("Kullanıcı kimliği zorunludur.");
        }

        if (request.MemberRole != ProjectMemberRole.Member)
        {
            return ProjectOperationResult<ProjectMemberDto>.Failure("Proje yöneticisi, proje oluşturulurken atanır ve bu uç noktadan değiştirilemez.");
        }

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null || !user.IsActive)
        {
            return ProjectOperationResult<ProjectMemberDto>.Failure("Eklenecek kullanıcı bulunamadı veya pasif durumda.");
        }

        var membership = await dbContext.ProjectMembers
            .SingleOrDefaultAsync(member => member.ProjectId == projectId && member.UserId == request.UserId, cancellationToken);

        if (membership is { IsActive: true })
        {
            return ProjectOperationResult<ProjectMemberDto>.Failure("Kullanıcı zaten projenin aktif üyesidir.");
        }

        if (membership is null)
        {
            membership = new ProjectMember
            {
                ProjectId = projectId,
                UserId = user.Id,
                MemberRole = ProjectMemberRole.Member,
                IsActive = true,
                JoinedDate = DateTime.UtcNow
            };
            dbContext.ProjectMembers.Add(membership);
        }
        else
        {
            membership.MemberRole = ProjectMemberRole.Member;
            membership.IsActive = true;
            membership.JoinedDate = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await activityLogService.WriteAsync(access.UserId, "Project", projectId, "MemberAdded", "Projeye üye eklendi.", cancellationToken);
        if (user.Id != access.UserId) await notificationService.NotifyAsync(user.Id, "Projeye eklendiniz", "Bir projeye üye olarak eklendiniz.", NotificationType.ProjectMemberAdded, projectId, cancellationToken);
        return ProjectOperationResult<ProjectMemberDto>.Success(ToDto(membership, user));
    }

    public async Task<ProjectOperationResult<ProjectMemberDto>> RemoveAsync(
        ProjectAccessContext access,
        int projectId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var project = await GetManageableProjectAsync(access, projectId, cancellationToken);
        if (project is null)
        {
            return ProjectOperationResult<ProjectMemberDto>.Failure("Proje bulunamadı veya üye yönetme yetkiniz yok.");
        }

        if (project.ManagerUserId == userId)
        {
            return ProjectOperationResult<ProjectMemberDto>.Failure("Proje yöneticisi üyelikten çıkarılamaz.");
        }

        var membership = await dbContext.ProjectMembers
            .SingleOrDefaultAsync(member => member.ProjectId == projectId && member.UserId == userId, cancellationToken);
        if (membership is null || !membership.IsActive)
        {
            return ProjectOperationResult<ProjectMemberDto>.Failure("Aktif proje üyeliği bulunamadı.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ProjectOperationResult<ProjectMemberDto>.Failure("Kullanıcı bulunamadı.");
        }

        membership.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        await activityLogService.WriteAsync(access.UserId, "Project", projectId, "MemberRemoved", "Proje üyeliği pasife çekildi.", cancellationToken);
        return ProjectOperationResult<ProjectMemberDto>.Success(ToDto(membership, user));
    }

    private async Task<bool> CanManageProjectAsync(ProjectAccessContext access, int projectId, CancellationToken cancellationToken) =>
        access.IsAdmin || await dbContext.Projects.AnyAsync(
            project => project.Id == projectId && !project.IsArchived && project.ManagerUserId == access.UserId,
            cancellationToken);

    private async Task<Project?> GetManageableProjectAsync(ProjectAccessContext access, int projectId, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.SingleOrDefaultAsync(
            candidate => candidate.Id == projectId && !candidate.IsArchived,
            cancellationToken);

        return project is not null && (access.IsAdmin || project.ManagerUserId == access.UserId) ? project : null;
    }

    private static ProjectMemberDto ToDto(ProjectMember member, ApplicationUser user) =>
        new(member.Id, user.Id, user.FirstName, user.LastName, user.Email ?? string.Empty, member.MemberRole, member.JoinedDate, member.IsActive);
}
