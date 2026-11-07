using System.ComponentModel.DataAnnotations;

namespace TeamTaskManager.Application.Users;

public sealed class CreateUserRequest
{
    [Required, StringLength(80)] public string FirstName { get; init; } = string.Empty;

    [Required, StringLength(80)] public string LastName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)] public string Email { get; init; } = string.Empty;

    [Required, MinLength(8)] public string Password { get; init; } = string.Empty;

    public IReadOnlyCollection<string>? Roles { get; init; }
}
