using TeamTaskManager.Application.Users;
using TeamTaskManager.Domain.Enums;
namespace TeamTaskManager.Application.Notifications;
public interface INotificationService
{
    Task NotifyAsync(string userId, string title, string message, NotificationType type, int? relatedEntityId, CancellationToken cancellationToken = default);
    Task<PagedResult<NotificationDto>> GetAsync(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> MarkReadAsync(string userId, int notificationId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default);
}
