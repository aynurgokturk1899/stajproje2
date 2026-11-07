using System.ComponentModel.DataAnnotations;

namespace TeamTaskManager.Application.Projects;

public sealed class CreateProjectRequest
{
    [Required, StringLength(150, MinimumLength = 3)] public string Name { get; init; } = string.Empty;

    [StringLength(1000)] public string? Description { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly TargetEndDate { get; init; }

    // Yalnızca Sistem Yöneticisi proje yöneticisini seçebilir.
    public string? ManagerUserId { get; init; }
}
