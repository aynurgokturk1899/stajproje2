using TeamTaskManager.Domain.Enums;

namespace TeamTaskManager.Domain.Entities;

public class Project
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly TargetEndDate { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;

    public string ManagerUserId { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
