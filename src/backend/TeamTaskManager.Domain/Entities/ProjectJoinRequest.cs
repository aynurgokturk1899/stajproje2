using TeamTaskManager.Domain.Enums;

namespace TeamTaskManager.Domain.Entities;

public class ProjectJoinRequest
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ProjectJoinRequestStatus Status { get; set; } = ProjectJoinRequestStatus.Pending;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedDate { get; set; }
    public string? ReviewedByUserId { get; set; }
    public Project Project { get; set; } = null!;
}
