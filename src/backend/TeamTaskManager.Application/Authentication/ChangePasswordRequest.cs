using System.ComponentModel.DataAnnotations;

namespace TeamTaskManager.Application.Authentication;

public sealed class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required, MinLength(8)]
    public string NewPassword { get; init; } = string.Empty;
}
