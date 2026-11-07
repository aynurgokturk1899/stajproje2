using TeamTaskManager.Domain.Enums;

namespace TeamTaskManager.Domain.Entities;

public class TaskItem
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string AssignedUserId { get; set; } = string.Empty;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.New;

    public DateOnly? StartDate { get; set; }

    public DateOnly DueDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Project Project { get; set; } = null!;

    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();

    public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
}
