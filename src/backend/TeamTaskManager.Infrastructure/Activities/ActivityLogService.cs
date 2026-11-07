using TeamTaskManager.Application.Activities;
using TeamTaskManager.Domain.Entities;
using TeamTaskManager.Infrastructure.Persistence;

namespace TeamTaskManager.Infrastructure.Activities;

public sealed class ActivityLogService(ApplicationDbContext dbContext) : IActivityLogService
{
    public async Task WriteAsync(string userId, string entityType, int entityId, string action, string description, CancellationToken cancellationToken = default)
    {
        dbContext.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Description = description
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
