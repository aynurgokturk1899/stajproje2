using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamTaskManager.Application.Projects;
using TeamTaskManager.Application.Activities;
using TeamTaskManager.Application.Users;
using TeamTaskManager.Domain.Entities;
using TeamTaskManager.Domain.Constants;
using TeamTaskManager.Domain.Enums;
using TeamTaskManager.Infrastructure.Persistence;

namespace TeamTaskManager.Infrastructure.Projects;

public sealed class ProjectManagementService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IActivityLogService activityLogService) : IProjectManagementService
{
    public async Task<PagedResult<ProjectDto>> GetProjectsAsync(ProjectAccessContext access, ProjectListQuery query, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var projects = dbContext.Projects.AsNoTracking().Where(project => !project.IsArchived);

        if (query.AvailableForJoin)
        {
            projects = projects.Where(project =>
                project.Status != ProjectStatus.Completed &&
                project.Status != ProjectStatus.Cancelled &&
                project.ManagerUserId != access.UserId &&
                !project.Members.Any(member => member.UserId == access.UserId && member.IsActive));
        }
        else if (!access.IsAdmin)
        {
            projects = projects.Where(project =>
                project.ManagerUserId == access.UserId ||
                project.Members.Any(member => member.UserId == access.UserId && member.IsActive));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            projects = projects.Where(project => project.Name.Contains(search));
        }

        if (query.Status.HasValue)
        {
            projects = projects.Where(project => project.Status == query.Status.Value);
        }

        var totalCount = await projects.CountAsync(cancellationToken);
        var items = await projects
            .OrderByDescending(project => project.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(project => ToDto(project))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectDto>(
            items,
            pageNumber,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<ProjectOperationResult<ProjectDto>> GetByIdAsync(ProjectAccessContext access, int projectId, CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects.AsNoTracking().SingleOrDefaultAsync(item => item.Id == projectId && !item.IsArchived, cancellationToken);
        if (project is null) return ProjectOperationResult<ProjectDto>.NotFound("Proje bulunamadı.");
        if (!access.IsAdmin && project.ManagerUserId != access.UserId &&
            !await dbContext.ProjectMembers.AnyAsync(member => member.ProjectId == projectId && member.UserId == access.UserId && member.IsActive, cancellationToken))
            return ProjectOperationResult<ProjectDto>.Forbidden("Bu projeye erişim yetkiniz yok.");
        return ProjectOperationResult<ProjectDto>.Success(ToDto(project));
    }

    public async Task<ProjectOperationResult<ProjectDto>> CreateAsync(ProjectAccessContext access, CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var errors = ProjectBusinessRules.Validate(request.Name, request.Description, request.StartDate, request.TargetEndDate).ToList();
        var managerUserId = access.IsAdmin && !string.IsNullOrWhiteSpace(request.ManagerUserId)
            ? request.ManagerUserId
            : access.UserId;
        var manager = await userManager.FindByIdAsync(managerUserId);
        if (manager is null || !manager.IsActive)
        {
            errors.Add("Aktif proje yöneticisi bulunamadı.");
        }

        if (errors.Count > 0)
        {
            return ProjectOperationResult<ProjectDto>.Failure(errors.ToArray());
        }

        if (access.IsAdmin && !await userManager.IsInRoleAsync(manager!, ApplicationRoles.ProjectManager))
        {
            var roleResult = await userManager.AddToRoleAsync(manager!, ApplicationRoles.ProjectManager);
            if (!roleResult.Succeeded)
            {
                return ProjectOperationResult<ProjectDto>.Failure(roleResult.Errors.Select(error => error.Description).ToArray());
            }
        }

        var duplicateExists = await dbContext.Projects.AnyAsync(
            project => project.ManagerUserId == managerUserId && project.Name == request.Name.Trim() && !project.IsArchived,
            cancellationToken);
        if (duplicateExists)
        {
            return ProjectOperationResult<ProjectDto>.Failure("Aynı yöneticiye ait aktif bir proje aynı ada sahip olamaz.");
        }

        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description),
            StartDate = request.StartDate,
            TargetEndDate = request.TargetEndDate,
            Status = ProjectStatus.Planned,
            ManagerUserId = managerUserId,
            Members =
            [
                new ProjectMember
                {
                    UserId = managerUserId,
                    MemberRole = ProjectMemberRole.Manager,
                    IsActive = true
                }
            ]
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);
        await activityLogService.WriteAsync(access.UserId, "Project", project.Id, "Created", "Proje oluşturuldu.", cancellationToken);
        return ProjectOperationResult<ProjectDto>.Success(ToDto(project));
    }

    public async Task<ProjectOperationResult<ProjectDto>> UpdateAsync(ProjectAccessContext access, int projectId, UpdateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = await GetManageableProjectQuery(access, projectId).SingleOrDefaultAsync(cancellationToken);
        if (project is null)
        {
            return ProjectOperationResult<ProjectDto>.Failure("Proje bulunamadı veya güncelleme yetkiniz yok.");
        }

        var errors = ProjectBusinessRules.Validate(request.Name, request.Description, request.StartDate, request.TargetEndDate).ToList();
        if (errors.Count > 0)
        {
            return ProjectOperationResult<ProjectDto>.Failure(errors.ToArray());
        }

        var duplicateExists = await dbContext.Projects.AnyAsync(
            candidate => candidate.Id != projectId &&
                         candidate.ManagerUserId == project.ManagerUserId &&
                         candidate.Name == request.Name.Trim() &&
                         !candidate.IsArchived,
            cancellationToken);
        if (duplicateExists)
        {
            return ProjectOperationResult<ProjectDto>.Failure("Aynı yöneticiye ait aktif bir proje aynı ada sahip olamaz.");
        }

        project.Name = request.Name.Trim();
        project.Description = NormalizeDescription(request.Description);
        project.StartDate = request.StartDate;
        project.TargetEndDate = request.TargetEndDate;
        project.UpdatedDate = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await activityLogService.WriteAsync(access.UserId, "Project", project.Id, "Updated", "Proje bilgileri güncellendi.", cancellationToken);
        return ProjectOperationResult<ProjectDto>.Success(ToDto(project));
    }

    public async Task<ProjectOperationResult<ProjectDto>> UpdateStatusAsync(ProjectAccessContext access, int projectId, ProjectStatus status, CancellationToken cancellationToken = default)
    {
        var project = await GetManageableProjectQuery(access, projectId).SingleOrDefaultAsync(cancellationToken);
        if (project is null)
        {
            return ProjectOperationResult<ProjectDto>.Failure("Proje bulunamadı veya güncelleme yetkiniz yok.");
        }

        if (!Enum.IsDefined(status))
        {
            return ProjectOperationResult<ProjectDto>.Failure("Geçersiz proje durumu.");
        }

        project.Status = status;
        project.UpdatedDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await activityLogService.WriteAsync(access.UserId, "Project", project.Id, "StatusChanged", "Proje durumu güncellendi.", cancellationToken);
        return ProjectOperationResult<ProjectDto>.Success(ToDto(project));
    }

    private IQueryable<Project> GetAccessibleProjectQuery(ProjectAccessContext access, int projectId)
    {
        var projects = dbContext.Projects.Where(project => project.Id == projectId && !project.IsArchived);
        return access.IsAdmin
            ? projects
            : projects.Where(project => project.ManagerUserId == access.UserId || project.Members.Any(member => member.UserId == access.UserId && member.IsActive));
    }

    private IQueryable<Project> GetManageableProjectQuery(ProjectAccessContext access, int projectId)
    {
        var projects = dbContext.Projects.Where(project => project.Id == projectId && !project.IsArchived);
        return access.IsAdmin ? projects : projects.Where(project => project.ManagerUserId == access.UserId);
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static ProjectDto ToDto(Project project) =>
        new(project.Id, project.Name, project.Description, project.StartDate, project.TargetEndDate, project.Status, project.ManagerUserId, project.CreatedDate, project.UpdatedDate);
}
