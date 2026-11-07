using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamTaskManager.Application.Notifications;
using TeamTaskManager.Application.Projects;
using TeamTaskManager.Domain.Entities;
using TeamTaskManager.Domain.Enums;
using TeamTaskManager.Infrastructure.Persistence;

namespace TeamTaskManager.Infrastructure.Projects;

public sealed class ProjectJoinRequestService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    INotificationService notificationService) : IProjectJoinRequestService
{
    public async Task<ProjectOperationResult<ProjectJoinRequestDto>> CreateAsync(string userId, int projectId, CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects.SingleOrDefaultAsync(p => p.Id == projectId && !p.IsArchived, cancellationToken);
        if (project is null) return ProjectOperationResult<ProjectJoinRequestDto>.Failure("Proje bulunamadı.");
        if (project.Status is ProjectStatus.Completed or ProjectStatus.Cancelled) return ProjectOperationResult<ProjectJoinRequestDto>.Failure("Tamamlanan veya iptal edilen projeye katılım isteği gönderilemez.");
        if (project.ManagerUserId == userId || await dbContext.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == userId && m.IsActive, cancellationToken))
            return ProjectOperationResult<ProjectJoinRequestDto>.Failure("Bu projenin zaten aktif bir üyesisin.");
        if (await dbContext.ProjectJoinRequests.AnyAsync(r => r.ProjectId == projectId && r.UserId == userId && r.Status == ProjectJoinRequestStatus.Pending, cancellationToken))
            return ProjectOperationResult<ProjectJoinRequestDto>.Failure("Bu proje için bekleyen bir katılım isteğin var.");

        var request = new ProjectJoinRequest { ProjectId = projectId, UserId = userId };
        dbContext.ProjectJoinRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);
        await notificationService.NotifyAsync(project.ManagerUserId, "Yeni katılım isteği", "Bir kullanıcı projenize katılmak istiyor.", NotificationType.ProjectMemberAdded, projectId, cancellationToken);
        return ProjectOperationResult<ProjectJoinRequestDto>.Success(await ToDtoAsync(request, cancellationToken));
    }

    public async Task<IReadOnlyCollection<ProjectJoinRequestDto>> GetMyAsync(string userId, CancellationToken cancellationToken = default)
    {
        var requests = await Query().Where(r => r.UserId == userId).OrderByDescending(r => r.CreatedDate).ToListAsync(cancellationToken);
        var results = new List<ProjectJoinRequestDto>(requests.Count);
        foreach (var request in requests)
        {
            results.Add(await ToDtoAsync(request, cancellationToken));
        }
        return results;
    }

    public async Task<IReadOnlyCollection<ProjectJoinRequestDto>> GetPendingAsync(ProjectAccessContext access, CancellationToken cancellationToken = default)
    {
        var query = Query().Where(r => r.Status == ProjectJoinRequestStatus.Pending);
        if (!access.IsAdmin) query = query.Where(r => r.Project.ManagerUserId == access.UserId);
        var requests = await query.OrderByDescending(r => r.CreatedDate).ToListAsync(cancellationToken);
        var results = new List<ProjectJoinRequestDto>(requests.Count);
        foreach (var request in requests)
        {
            results.Add(await ToDtoAsync(request, cancellationToken));
        }
        return results;
    }

    public async Task<ProjectOperationResult<ProjectJoinRequestDto>> ReviewAsync(ProjectAccessContext access, int requestId, bool approve, CancellationToken cancellationToken = default)
    {
        var request = await dbContext.ProjectJoinRequests.Include(r => r.Project).SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (request is null || request.Status != ProjectJoinRequestStatus.Pending)
            return ProjectOperationResult<ProjectJoinRequestDto>.Failure("Bekleyen katılım isteği bulunamadı.");
        if (!access.IsAdmin && request.Project.ManagerUserId != access.UserId)
            return ProjectOperationResult<ProjectJoinRequestDto>.Failure("Bu isteği yönetme yetkiniz yok.");

        request.Status = approve ? ProjectJoinRequestStatus.Approved : ProjectJoinRequestStatus.Rejected;
        request.ReviewedByUserId = access.UserId;
        request.ReviewedDate = DateTime.UtcNow;
        if (approve)
        {
            var member = await dbContext.ProjectMembers.SingleOrDefaultAsync(m => m.ProjectId == request.ProjectId && m.UserId == request.UserId, cancellationToken);
            if (member is null) dbContext.ProjectMembers.Add(new ProjectMember { ProjectId = request.ProjectId, UserId = request.UserId, MemberRole = ProjectMemberRole.Member });
            else { member.IsActive = true; member.JoinedDate = DateTime.UtcNow; }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await notificationService.NotifyAsync(request.UserId, approve ? "Katılım isteğin onaylandı" : "Katılım isteğin reddedildi", approve ? $"{request.Project.Name} projesine eklendin." : $"{request.Project.Name} projesine katılım isteğin reddedildi.", NotificationType.ProjectMemberAdded, request.ProjectId, cancellationToken);
        return ProjectOperationResult<ProjectJoinRequestDto>.Success(await ToDtoAsync(request, cancellationToken));
    }

    private IQueryable<ProjectJoinRequest> Query() => dbContext.ProjectJoinRequests.AsNoTracking().Include(r => r.Project);
    private async Task<ProjectJoinRequestDto> ToDtoAsync(ProjectJoinRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId) ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");
        return new ProjectJoinRequestDto(request.Id, request.ProjectId, request.Project.Name, request.UserId, user.FirstName, user.LastName, user.Email ?? string.Empty, request.Status, request.CreatedDate);
    }
}
