using System.ComponentModel.DataAnnotations;
using TeamTaskManager.Domain.Enums;

namespace TeamTaskManager.Application.Tasks;

public sealed class UpdateTaskRequest
{
    [Required, StringLength(200, MinimumLength = 3)] public string Title { get; init; } = string.Empty;
    [StringLength(5000)] public string? Description { get; init; }
    [Required] public string AssignedUserId { get; init; } = string.Empty;
    public TaskPriority Priority { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly DueDate { get; init; }
}
