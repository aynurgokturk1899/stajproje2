namespace TeamTaskManager.Application.Authentication;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    AuthenticatedUser User);
