using TeamTaskManager.Domain.Enums;

namespace TeamTaskManager.Domain.Entities;

public class Notification
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    public int? RelatedEntityId { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
