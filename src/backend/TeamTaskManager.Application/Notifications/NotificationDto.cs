using TeamTaskManager.Domain.Enums;
namespace TeamTaskManager.Application.Notifications;
public sealed record NotificationDto(int Id, string Title, string Message, NotificationType Type, int? RelatedEntityId, bool IsRead, DateTime? ReadDate, DateTime CreatedDate);
