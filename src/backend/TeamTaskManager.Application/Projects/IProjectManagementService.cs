using TeamTaskManager.Application.Users;
using TeamTaskManager.Domain.Enums;

namespace TeamTaskManager.Application.Projects;

public interface IProjectManagementService
{
    Task<PagedResult<ProjectDto>> GetProjectsAsync(ProjectAccessContext access, ProjectListQuery query, CancellationToken cancellationToken = default);

    Task<ProjectOperationResult<ProjectDto>> GetByIdAsync(ProjectAccessContext access, int projectId, CancellationToken cancellationToken = default);

    Task<ProjectOperationResult<ProjectDto>> CreateAsync(ProjectAccessContext access, CreateProjectRequest request, CancellationToken cancellationToken = default);

    Task<ProjectOperationResult<ProjectDto>> UpdateAsync(ProjectAccessContext access, int projectId, UpdateProjectRequest request, CancellationToken cancellationToken = default);

    Task<ProjectOperationResult<ProjectDto>> UpdateStatusAsync(ProjectAccessContext access, int projectId, ProjectStatus status, CancellationToken cancellationToken = default);
}
