using TeamTaskManager.Domain.Enums;

namespace TeamTaskManager.Application.Projects;

public sealed class ProjectListQuery
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    public ProjectStatus? Status { get; init; }

    public bool AvailableForJoin { get; init; }
}
