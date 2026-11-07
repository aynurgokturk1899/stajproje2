using TaskPriority = TeamTaskManager.Domain.Enums.TaskPriority;
using System.ComponentModel.DataAnnotations;

namespace TeamTaskManager.Application.Tasks;

public sealed class CreateTaskRequest
{
    [Range(1, int.MaxValue)] public int ProjectId { get; init; }
    [Required, StringLength(200, MinimumLength = 3)] public string Title { get; init; } = string.Empty;
    [StringLength(5000)] public string? Description { get; init; }
    [Required] public string AssignedUserId { get; init; } = string.Empty;
    public TaskPriority Priority { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly DueDate { get; init; }
}
