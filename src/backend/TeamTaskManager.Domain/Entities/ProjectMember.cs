using TeamTaskManager.Domain.Enums;

namespace TeamTaskManager.Domain.Entities;

public class ProjectMember
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ProjectMemberRole MemberRole { get; set; } = ProjectMemberRole.Member;

    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public Project Project { get; set; } = null!;
}
