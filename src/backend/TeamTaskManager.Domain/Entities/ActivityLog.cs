namespace TeamTaskManager.Domain.Entities;

public class ActivityLog
{
    public long Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
