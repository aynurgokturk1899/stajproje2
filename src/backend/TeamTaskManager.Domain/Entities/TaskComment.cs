namespace TeamTaskManager.Domain.Entities;

public class TaskComment
{
    public int Id { get; set; }

    public int TaskItemId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }

    public TaskItem TaskItem { get; set; } = null!;
}
