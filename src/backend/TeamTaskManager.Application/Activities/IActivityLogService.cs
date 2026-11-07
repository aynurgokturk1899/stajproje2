namespace TeamTaskManager.Application.Activities;

public interface IActivityLogService
{
    Task WriteAsync(string userId, string entityType, int entityId, string action, string description, CancellationToken cancellationToken = default);
}
