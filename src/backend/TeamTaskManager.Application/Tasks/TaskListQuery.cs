using TaskPriority = TeamTaskManager.Domain.Enums.TaskPriority;
using TaskStatus = TeamTaskManager.Domain.Enums.TaskStatus;

namespace TeamTaskManager.Application.Tasks;

public sealed class TaskListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int? ProjectId { get; init; }
    public string? AssignedUserId { get; init; }
    public TaskStatus? Status { get; init; }
    public TaskPriority? Priority { get; init; }
    public DateOnly? DueFrom { get; init; }
    public DateOnly? DueTo { get; init; }
    public string? Sort { get; init; }
}
