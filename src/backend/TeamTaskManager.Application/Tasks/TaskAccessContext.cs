namespace TeamTaskManager.Application.Tasks;

public sealed record TaskAccessContext(string UserId, bool IsAdmin, bool IsProjectManager);
