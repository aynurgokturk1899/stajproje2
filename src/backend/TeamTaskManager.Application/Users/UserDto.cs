namespace TeamTaskManager.Application.Users;

public sealed record UserDto(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    DateTime CreatedDate,
    IReadOnlyCollection<string> Roles);
