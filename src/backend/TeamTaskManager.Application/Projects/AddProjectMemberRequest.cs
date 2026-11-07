using TeamTaskManager.Domain.Enums;

namespace TeamTaskManager.Application.Projects;

public sealed class AddProjectMemberRequest
{
    public string UserId { get; init; } = string.Empty;

    public ProjectMemberRole MemberRole { get; init; } = ProjectMemberRole.Member;
}
