using TeamTaskManager.Domain.Enums;

namespace TeamTaskManager.Application.Projects;

public sealed class UpdateProjectStatusRequest
{
    public ProjectStatus Status { get; init; }
}
