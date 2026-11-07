namespace TeamTaskManager.Application.Authentication;

public sealed record AuthenticatedUser(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyCollection<string> Roles,
    bool IsActive);
