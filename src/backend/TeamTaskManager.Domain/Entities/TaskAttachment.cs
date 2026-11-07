namespace TeamTaskManager.Domain.Entities;

public class TaskAttachment
{
    public int Id { get; set; }

    public int TaskItemId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string UploadedByUserId { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public TaskItem TaskItem { get; set; } = null!;
}
