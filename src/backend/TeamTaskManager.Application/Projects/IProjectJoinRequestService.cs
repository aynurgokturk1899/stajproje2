namespace TeamTaskManager.Application.Projects;

public interface IProjectJoinRequestService
{
    Task<ProjectOperationResult<ProjectJoinRequestDto>> CreateAsync(string userId, int projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProjectJoinRequestDto>> GetMyAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProjectJoinRequestDto>> GetPendingAsync(ProjectAccessContext access, CancellationToken cancellationToken = default);
    Task<ProjectOperationResult<ProjectJoinRequestDto>> ReviewAsync(ProjectAccessContext access, int requestId, bool approve, CancellationToken cancellationToken = default);
}
