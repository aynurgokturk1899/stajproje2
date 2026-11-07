using Microsoft.EntityFrameworkCore;
using TeamTaskManager.Application.Notifications;
using TeamTaskManager.Application.Users;
using TeamTaskManager.Domain.Entities;
using TeamTaskManager.Domain.Enums;
using TeamTaskManager.Infrastructure.Persistence;
namespace TeamTaskManager.Infrastructure.Notifications;
public sealed class NotificationService(ApplicationDbContext dbContext) : INotificationService
{
    public async Task NotifyAsync(string userId, string title, string message, NotificationType type, int? relatedEntityId, CancellationToken cancellationToken = default)
    {
        dbContext.Notifications.Add(new Notification { UserId = userId, Title = title, Message = message, Type = type, RelatedEntityId = relatedEntityId });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    public async Task<PagedResult<NotificationDto>> GetAsync(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber); pageSize = Math.Clamp(pageSize, 1, 100);
        var items = dbContext.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        var total = await items.CountAsync(cancellationToken);
        var page = await items.OrderByDescending(n => n.CreatedDate).Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.Type, n.RelatedEntityId, n.IsRead, n.ReadDate, n.CreatedDate)).ToListAsync(cancellationToken);
        return new PagedResult<NotificationDto>(page, pageNumber, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }
    public async Task<bool> MarkReadAsync(string userId, int notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await dbContext.Notifications.SingleOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);
        if (notification is null) return false;
        if (!notification.IsRead) { notification.IsRead = true; notification.ReadDate = DateTime.UtcNow; await dbContext.SaveChangesAsync(cancellationToken); }
        return true;
    }
    public async Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var unread = await dbContext.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(cancellationToken);
        foreach (var notification in unread) { notification.IsRead = true; notification.ReadDate = DateTime.UtcNow; }
        if (unread.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);
    }
}
