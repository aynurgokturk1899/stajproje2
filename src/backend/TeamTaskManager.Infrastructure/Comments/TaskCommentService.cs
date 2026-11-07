using Microsoft.EntityFrameworkCore;
using TeamTaskManager.Application.Activities;
using TeamTaskManager.Application.Comments;
using TeamTaskManager.Application.Tasks;
using TeamTaskManager.Application.Notifications;
using TeamTaskManager.Domain.Enums;
using TeamTaskManager.Domain.Entities;
using TeamTaskManager.Infrastructure.Persistence;

namespace TeamTaskManager.Infrastructure.Comments;

public sealed class TaskCommentService(
    ApplicationDbContext dbContext,
    IActivityLogService activityLogService,
    INotificationService notificationService) : ITaskCommentService
{
    public async Task<IReadOnlyCollection<TaskCommentDto>?> GetCommentsAsync(TaskAccessContext access, int taskId, CancellationToken cancellationToken = default)
    {
        if (!await HasProjectAccessAsync(access, taskId, cancellationToken)) return null;

        return await (from comment in dbContext.TaskComments.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on comment.UserId equals user.Id
            where comment.TaskItemId == taskId
            orderby comment.CreatedDate
            select new TaskCommentDto(comment.Id, comment.TaskItemId, comment.UserId,
                user.FirstName + " " + user.LastName, user.Email ?? string.Empty,
                (from userRole in dbContext.UserRoles
                 join role in dbContext.Roles on userRole.RoleId equals role.Id
                 where userRole.UserId == user.Id
                 orderby role.Name == "Admin" ? 0 : role.Name == "ProjectManager" ? 1 : 2
                 select role.Name!).FirstOrDefault() ?? "TeamMember",
                comment.Content, comment.CreatedDate, comment.UpdatedDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<CommentOperationResult<TaskCommentDto>> CreateAsync(TaskAccessContext access, int taskId, CreateTaskCommentRequest request, CancellationToken cancellationToken = default)
    {
        if (!await HasProjectAccessAsync(access, taskId, cancellationToken))
        {
            return CommentOperationResult<TaskCommentDto>.Failure("Görev bulunamadı veya yorum ekleme yetkiniz yok.");
        }

        var content = request.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content) || content.Length > 1500)
        {
            return CommentOperationResult<TaskCommentDto>.Failure("Yorum boş olamaz ve en fazla 1500 karakter olabilir.");
        }

        var comment = new TaskComment { TaskItemId = taskId, UserId = access.UserId, Content = content };
        dbContext.TaskComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
        await activityLogService.WriteAsync(access.UserId, "TaskItem", taskId, "CommentCreated", "Göreve yorum eklendi.", cancellationToken);
        var taskInfo = await dbContext.TaskItems.Where(task => task.Id == taskId)
            .Select(task => new { task.Title, task.AssignedUserId, task.Project.ManagerUserId })
            .SingleAsync(cancellationToken);
        var authorName = await dbContext.Users.Where(user => user.Id == access.UserId)
            .Select(user => user.FirstName + " " + user.LastName)
            .SingleAsync(cancellationToken);
        var adminUserIds = await (from user in dbContext.Users
            join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where user.IsActive && role.Name == "Admin"
            select user.Id).ToArrayAsync(cancellationToken);
        var recipients = new[] { taskInfo.AssignedUserId, taskInfo.ManagerUserId }
            .Concat(adminUserIds)
            .Where(userId => userId != access.UserId)
            .Distinct()
            .ToArray();
        foreach (var recipientId in recipients)
        {
            await notificationService.NotifyAsync(recipientId, "Göreve yeni yorum geldi",
                $"{authorName}, '{taskInfo.Title}' görevine yorum yazdı.",
                NotificationType.CommentAdded, taskId, cancellationToken);
        }
        return CommentOperationResult<TaskCommentDto>.Success(await ToDtoAsync(comment, cancellationToken));
    }

    public async Task<CommentOperationResult<TaskCommentDto>> UpdateAsync(TaskAccessContext access, int commentId, UpdateTaskCommentRequest request, CancellationToken cancellationToken = default)
    {
        var comment = await dbContext.TaskComments.SingleOrDefaultAsync(candidate => candidate.Id == commentId, cancellationToken);
        if (comment is null || comment.UserId != access.UserId)
        {
            return CommentOperationResult<TaskCommentDto>.Failure("Yorum bulunamadı veya güncelleme yetkiniz yok.");
        }

        var content = request.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content) || content.Length > 1500)
        {
            return CommentOperationResult<TaskCommentDto>.Failure("Yorum boş olamaz ve en fazla 1500 karakter olabilir.");
        }

        comment.Content = content;
        comment.UpdatedDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await activityLogService.WriteAsync(access.UserId, "TaskItem", comment.TaskItemId, "CommentUpdated", "Görev yorumu güncellendi.", cancellationToken);
        return CommentOperationResult<TaskCommentDto>.Success(await ToDtoAsync(comment, cancellationToken));
    }

    public async Task<CommentOperationResult<TaskCommentDto>> DeleteAsync(TaskAccessContext access, int commentId, CancellationToken cancellationToken = default)
    {
        var comment = await dbContext.TaskComments.SingleOrDefaultAsync(candidate => candidate.Id == commentId, cancellationToken);
        if (comment is null || comment.UserId != access.UserId)
        {
            return CommentOperationResult<TaskCommentDto>.Failure("Yorum bulunamadı veya silme yetkiniz yok.");
        }

        var result = await ToDtoAsync(comment, cancellationToken);
        dbContext.TaskComments.Remove(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
        await activityLogService.WriteAsync(access.UserId, "TaskItem", result.TaskItemId, "CommentDeleted", "Görev yorumu silindi.", cancellationToken);
        return CommentOperationResult<TaskCommentDto>.Success(result);
    }

    private Task<bool> HasProjectAccessAsync(TaskAccessContext access, int taskId, CancellationToken cancellationToken) =>
        dbContext.TaskItems.AnyAsync(task => task.Id == taskId && !task.IsArchived &&
            (access.IsAdmin || task.Project.ManagerUserId == access.UserId || task.Project.Members.Any(member => member.UserId == access.UserId && member.IsActive)), cancellationToken);

    private async Task<TaskCommentDto> ToDtoAsync(TaskComment comment, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking()
            .Where(candidate => candidate.Id == comment.UserId)
            .Select(candidate => new { candidate.FirstName, candidate.LastName, candidate.Email })
            .SingleAsync(cancellationToken);
        var role = await (from userRole in dbContext.UserRoles.AsNoTracking()
            join applicationRole in dbContext.Roles.AsNoTracking() on userRole.RoleId equals applicationRole.Id
            where userRole.UserId == comment.UserId
            orderby applicationRole.Name == "Admin" ? 0 : applicationRole.Name == "ProjectManager" ? 1 : 2
            select applicationRole.Name!).FirstOrDefaultAsync(cancellationToken) ?? "TeamMember";
        return new TaskCommentDto(comment.Id, comment.TaskItemId, comment.UserId,
            $"{user.FirstName} {user.LastName}".Trim(), user.Email ?? string.Empty,
            role,
            comment.Content, comment.CreatedDate, comment.UpdatedDate);
    }
}
