using TaskStatus = TeamTaskManager.Domain.Enums.TaskStatus;

namespace TeamTaskManager.Application.Tasks;

public sealed class UpdateTaskStatusRequest
{
    public TaskStatus Status { get; init; }
}
