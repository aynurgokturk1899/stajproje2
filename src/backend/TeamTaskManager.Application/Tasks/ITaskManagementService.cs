using TeamTaskManager.Application.Users;

namespace TeamTaskManager.Application.Tasks;

public interface ITaskManagementService
{
    Task<PagedResult<TaskItemDto>> GetTasksAsync(TaskAccessContext access, TaskListQuery query, CancellationToken cancellationToken = default);
    Task<PagedResult<TaskItemDto>> GetMyTasksAsync(TaskAccessContext access, TaskListQuery query, CancellationToken cancellationToken = default);
    Task<TaskOperationResult<TaskItemDto>> GetByIdAsync(TaskAccessContext access, int taskId, CancellationToken cancellationToken = default);
    Task<TaskOperationResult<TaskItemDto>> CreateAsync(TaskAccessContext access, CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<TaskOperationResult<TaskItemDto>> UpdateAsync(TaskAccessContext access, int taskId, UpdateTaskRequest request, CancellationToken cancellationToken = default);
    Task<TaskOperationResult<TaskItemDto>> UpdateStatusAsync(TaskAccessContext access, int taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken = default);
    Task<TaskOperationResult<TaskItemDto>> ArchiveAsync(TaskAccessContext access, int taskId, CancellationToken cancellationToken = default);
}
