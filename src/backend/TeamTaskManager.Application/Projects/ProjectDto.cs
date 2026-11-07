using TeamTaskManager.Domain.Enums;

namespace TeamTaskManager.Application.Projects;

public sealed record ProjectDto(
    int Id,
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly TargetEndDate,
    ProjectStatus Status,
    string ManagerUserId,
    DateTime CreatedDate,
    DateTime? UpdatedDate);
