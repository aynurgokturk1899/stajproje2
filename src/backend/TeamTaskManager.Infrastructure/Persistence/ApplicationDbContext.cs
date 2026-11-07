using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeamTaskManager.Domain.Constants;
using TeamTaskManager.Domain.Entities;

namespace TeamTaskManager.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProjectJoinRequest> ProjectJoinRequests => Set<ProjectJoinRequest>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FirstName).HasMaxLength(80).IsRequired();
            entity.Property(user => user.LastName).HasMaxLength(80).IsRequired();
            entity.Property(user => user.IsActive).HasDefaultValue(true);
            entity.Property(user => user.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        builder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = "7b3c2144-4d95-4b60-b35a-ecdbfcd66fb7",
                Name = ApplicationRoles.Admin,
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "e4c55be2-99b0-4542-96b7-6aaf7ec299a1"
            },
            new IdentityRole
            {
                Id = "01768d87-e963-4b77-b7da-b4a5916fbe46",
                Name = ApplicationRoles.ProjectManager,
                NormalizedName = "PROJECTMANAGER",
                ConcurrencyStamp = "2f400038-b4ca-4d7d-b0fc-8e5dded4628d"
            },
            new IdentityRole
            {
                Id = "a74af48a-beb5-4de1-a93a-cc91545bb5de",
                Name = ApplicationRoles.TeamMember,
                NormalizedName = "TEAMMEMBER",
                ConcurrencyStamp = "dc2934a0-7ea7-4354-b8f6-e690c4d5518c"
            });

        builder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects", table => table.HasCheckConstraint("CK_Projects_TargetEndDate", "[TargetEndDate] >= [StartDate]"));
            entity.Property(project => project.Name).HasMaxLength(150).IsRequired();
            entity.Property(project => project.Description).HasMaxLength(1000);
            entity.Property(project => project.StartDate).HasColumnType("date");
            entity.Property(project => project.TargetEndDate).HasColumnType("date");
            entity.Property(project => project.Status).HasConversion<byte>();
            entity.Property(project => project.ManagerUserId).HasMaxLength(450).IsRequired();
            entity.Property(project => project.IsArchived).HasDefaultValue(false);
            entity.Property(project => project.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(project => project.RowVersion).IsRowVersion();
            entity.HasIndex(project => new { project.ManagerUserId, project.IsArchived, project.Status });
            entity.HasIndex(project => new { project.ManagerUserId, project.Name }).IsUnique().HasFilter("[IsArchived] = 0");
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(project => project.ManagerUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProjectMember>(entity =>
        {
            entity.ToTable("ProjectMembers");
            entity.Property(member => member.UserId).HasMaxLength(450).IsRequired();
            entity.Property(member => member.MemberRole).HasConversion<byte>();
            entity.Property(member => member.JoinedDate).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(member => member.IsActive).HasDefaultValue(true);
            entity.HasIndex(member => new { member.ProjectId, member.UserId }).IsUnique();
            entity.HasIndex(member => new { member.UserId, member.IsActive });
            entity.HasOne(member => member.Project).WithMany(project => project.Members).HasForeignKey(member => member.ProjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(member => member.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProjectJoinRequest>(entity =>
        {
            entity.ToTable("ProjectJoinRequests");
            entity.Property(request => request.UserId).HasMaxLength(450).IsRequired();
            entity.Property(request => request.ReviewedByUserId).HasMaxLength(450);
            entity.Property(request => request.Status).HasConversion<byte>();
            entity.Property(request => request.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(request => new { request.ProjectId, request.Status });
            entity.HasIndex(request => new { request.UserId, request.Status });
            entity.HasOne(request => request.Project).WithMany().HasForeignKey(request => request.ProjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(request => request.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(request => request.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("TaskItems", table => table.HasCheckConstraint("CK_TaskItems_DueDate", "[StartDate] IS NULL OR [DueDate] >= [StartDate]"));
            entity.Property(task => task.Title).HasMaxLength(200).IsRequired();
            entity.Property(task => task.StartDate).HasColumnType("date");
            entity.Property(task => task.DueDate).HasColumnType("date");
            entity.Property(task => task.Priority).HasConversion<byte>();
            entity.Property(task => task.Status).HasConversion<byte>();
            entity.Property(task => task.AssignedUserId).HasMaxLength(450).IsRequired();
            entity.Property(task => task.CreatedByUserId).HasMaxLength(450).IsRequired();
            entity.Property(task => task.IsArchived).HasDefaultValue(false);
            entity.Property(task => task.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(task => task.RowVersion).IsRowVersion();
            entity.HasIndex(task => new { task.ProjectId, task.Status, task.IsArchived });
            entity.HasIndex(task => new { task.AssignedUserId, task.Status, task.DueDate });
            entity.HasOne(task => task.Project).WithMany(project => project.Tasks).HasForeignKey(task => task.ProjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(task => task.AssignedUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(task => task.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TaskComment>(entity =>
        {
            entity.ToTable("TaskComments");
            entity.Property(comment => comment.UserId).HasMaxLength(450).IsRequired();
            entity.Property(comment => comment.Content).HasMaxLength(1500).IsRequired();
            entity.Property(comment => comment.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(comment => new { comment.TaskItemId, comment.CreatedDate });
            entity.HasOne(comment => comment.TaskItem).WithMany(task => task.Comments).HasForeignKey(comment => comment.TaskItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(comment => comment.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TaskAttachment>(entity =>
        {
            entity.ToTable("TaskAttachments", table => table.HasCheckConstraint("CK_TaskAttachments_FileSize", "[FileSize] > 0 AND [FileSize] <= 5242880"));
            entity.Property(attachment => attachment.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(attachment => attachment.StoredFileName).HasMaxLength(255).IsRequired();
            entity.Property(attachment => attachment.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(attachment => attachment.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(attachment => attachment.UploadedByUserId).HasMaxLength(450).IsRequired();
            entity.Property(attachment => attachment.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(attachment => new { attachment.TaskItemId, attachment.CreatedDate });
            entity.HasOne(attachment => attachment.TaskItem).WithMany(task => task.Attachments).HasForeignKey(attachment => attachment.TaskItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(attachment => attachment.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.Property(notification => notification.UserId).HasMaxLength(450).IsRequired();
            entity.Property(notification => notification.Title).HasMaxLength(150).IsRequired();
            entity.Property(notification => notification.Message).HasMaxLength(500).IsRequired();
            entity.Property(notification => notification.Type).HasConversion<byte>();
            entity.Property(notification => notification.IsRead).HasDefaultValue(false);
            entity.Property(notification => notification.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(notification => new { notification.UserId, notification.IsRead, notification.CreatedDate });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(notification => notification.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ActivityLog>(entity =>
        {
            entity.ToTable("ActivityLogs");
            entity.Property(log => log.EntityType).HasMaxLength(60).IsRequired();
            entity.Property(log => log.Action).HasMaxLength(80).IsRequired();
            entity.Property(log => log.Description).HasMaxLength(500).IsRequired();
            entity.Property(log => log.UserId).HasMaxLength(450).IsRequired();
            entity.Property(log => log.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(log => new { log.EntityType, log.EntityId, log.CreatedDate });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(log => log.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
