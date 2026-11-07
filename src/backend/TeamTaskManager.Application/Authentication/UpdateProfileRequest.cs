using System.ComponentModel.DataAnnotations;

namespace TeamTaskManager.Application.Authentication;

public sealed class UpdateProfileRequest
{
    [Required, StringLength(80)] public string FirstName { get; init; } = string.Empty;
    [Required, StringLength(80)] public string LastName { get; init; } = string.Empty;
    [Phone, StringLength(30)] public string? PhoneNumber { get; init; }
}
