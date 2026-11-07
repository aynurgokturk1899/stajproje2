using TaskPriority = TeamTaskManager.Domain.Enums.TaskPriority;
using TaskStatus = TeamTaskManager.Domain.Enums.TaskStatus;

namespace TeamTaskManager.Application.Tasks;

public sealed record TaskItemDto(
    int Id,
    int ProjectId,
    string Title,
    string? Description,
    string AssignedUserId,
    TaskPriority Priority,
    TaskStatus Status,
    DateOnly? StartDate,
    DateOnly DueDate,
    DateTime? CompletedDate,
    string CreatedByUserId,
    DateTime CreatedDate,
    DateTime? UpdatedDate);
