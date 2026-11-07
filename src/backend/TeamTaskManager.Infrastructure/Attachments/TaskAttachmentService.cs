using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TeamTaskManager.Application.Activities;
using TeamTaskManager.Application.Attachments;
using TeamTaskManager.Application.Tasks;
using TeamTaskManager.Domain.Entities;
using TeamTaskManager.Infrastructure.Persistence;

namespace TeamTaskManager.Infrastructure.Attachments;

public sealed class TaskAttachmentService(
    ApplicationDbContext dbContext,
    IOptions<FileStorageOptions> fileStorageOptions,
    IActivityLogService activityLogService) : ITaskAttachmentService
{
    private static readonly IReadOnlyDictionary<string, string> ContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".txt"] = "text/plain"
    };

    private readonly FileStorageOptions _options = fileStorageOptions.Value;

    public async Task<IReadOnlyCollection<AttachmentDto>?> GetForTaskAsync(TaskAccessContext access, int taskId, CancellationToken cancellationToken = default)
    {
        if (!await HasProjectAccessAsync(access, taskId, cancellationToken)) return null;
        return await dbContext.TaskAttachments.AsNoTracking()
            .Where(item => item.TaskItemId == taskId)
            .OrderByDescending(item => item.CreatedDate)
            .Select(item => new AttachmentDto(item.Id, item.TaskItemId, item.OriginalFileName, item.ContentType, item.FileSize, item.UploadedByUserId, item.CreatedDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<AttachmentOperationResult<AttachmentDto>> UploadAsync(TaskAccessContext access, int taskId, UploadAttachmentCommand command, CancellationToken cancellationToken = default)
    {
        if (!await HasProjectAccessAsync(access, taskId, cancellationToken))
        {
            return AttachmentOperationResult<AttachmentDto>.Failure("Görev bulunamadı veya dosya yükleme yetkiniz yok.");
        }

        var validationError = Validate(command);
        if (validationError is not null)
        {
            return command.FileSize > _options.MaxFileSizeBytes
                ? AttachmentOperationResult<AttachmentDto>.FileTooLarge(validationError)
                : AttachmentOperationResult<AttachmentDto>.Failure(validationError);
        }

        var originalFileName = Path.GetFileName(command.OriginalFileName);
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine("tasks", taskId.ToString(), storedFileName);
        var absolutePath = Path.Combine(GetRootPath(), relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        try
        {
            await using (var destination = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await command.Content.CopyToAsync(destination, cancellationToken);
            }

            var attachment = new TaskAttachment
            {
                TaskItemId = taskId,
                OriginalFileName = originalFileName,
                StoredFileName = storedFileName,
                FilePath = relativePath,
                ContentType = ContentTypes[extension],
                FileSize = command.FileSize,
                UploadedByUserId = access.UserId
            };

            dbContext.TaskAttachments.Add(attachment);
            await dbContext.SaveChangesAsync(cancellationToken);
            await activityLogService.WriteAsync(access.UserId, "TaskItem", taskId, "AttachmentUploaded", "Göreve dosya eklendi.", cancellationToken);
            return AttachmentOperationResult<AttachmentDto>.Success(ToDto(attachment));
        }
        catch
        {
            if (File.Exists(absolutePath)) File.Delete(absolutePath);
            throw;
        }
    }

    public async Task<AttachmentOperationResult<AttachmentDownload>> DownloadAsync(TaskAccessContext access, int attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await GetAccessibleAttachmentAsync(access, attachmentId, cancellationToken);
        if (attachment is null) return AttachmentOperationResult<AttachmentDownload>.Failure("Dosya bulunamadı veya indirme yetkiniz yok.");

        var absolutePath = Path.Combine(GetRootPath(), attachment.FilePath);
        if (!File.Exists(absolutePath)) return AttachmentOperationResult<AttachmentDownload>.Failure("Dosya depolama alanında bulunamadı.");

        var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return AttachmentOperationResult<AttachmentDownload>.Success(new AttachmentDownload(stream, attachment.OriginalFileName, attachment.ContentType));
    }

    public async Task<AttachmentOperationResult<AttachmentDto>> DeleteAsync(TaskAccessContext access, int attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await dbContext.TaskAttachments.Include(item => item.TaskItem).ThenInclude(task => task.Project).SingleOrDefaultAsync(item => item.Id == attachmentId, cancellationToken);
        if (attachment is null || !CanDelete(access, attachment)) return AttachmentOperationResult<AttachmentDto>.Failure("Dosya bulunamadı veya silme yetkiniz yok.");

        var result = ToDto(attachment);
        var absolutePath = Path.Combine(GetRootPath(), attachment.FilePath);
        dbContext.TaskAttachments.Remove(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (File.Exists(absolutePath)) File.Delete(absolutePath);
        await activityLogService.WriteAsync(access.UserId, "TaskItem", attachment.TaskItemId, "AttachmentDeleted", "Görev dosyası silindi.", cancellationToken);
        return AttachmentOperationResult<AttachmentDto>.Success(result);
    }

    private string? Validate(UploadAttachmentCommand command)
    {
        if (command.FileSize <= 0) return "Boş dosya yüklenemez.";
        if (command.FileSize > _options.MaxFileSizeBytes) return "Dosya boyutu 5 MB sınırını aşıyor.";
        var extension = Path.GetExtension(Path.GetFileName(command.OriginalFileName));
        if (string.IsNullOrWhiteSpace(extension) || !_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) return "Dosya uzantısına izin verilmiyor.";
        if (!ContentTypes.TryGetValue(extension, out var expectedContentType) || !string.Equals(command.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase)) return "Dosya MIME türü geçersiz.";
        return null;
    }

    private Task<TaskAttachment?> GetAccessibleAttachmentAsync(TaskAccessContext access, int attachmentId, CancellationToken cancellationToken) =>
        dbContext.TaskAttachments.AsNoTracking().SingleOrDefaultAsync(item => item.Id == attachmentId &&
            (access.IsAdmin || item.TaskItem.Project.ManagerUserId == access.UserId || item.TaskItem.Project.Members.Any(member => member.UserId == access.UserId && member.IsActive)), cancellationToken);

    private Task<bool> HasProjectAccessAsync(TaskAccessContext access, int taskId, CancellationToken cancellationToken) =>
        dbContext.TaskItems.AnyAsync(task => task.Id == taskId && !task.IsArchived &&
            (access.IsAdmin || task.Project.ManagerUserId == access.UserId || task.Project.Members.Any(member => member.UserId == access.UserId && member.IsActive)), cancellationToken);

    private static bool CanDelete(TaskAccessContext access, TaskAttachment attachment) =>
        access.IsAdmin || attachment.UploadedByUserId == access.UserId || attachment.TaskItem.Project.ManagerUserId == access.UserId;

    private string GetRootPath() => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _options.RootPath));
    private static AttachmentDto ToDto(TaskAttachment attachment) => new(attachment.Id, attachment.TaskItemId, attachment.OriginalFileName, attachment.ContentType, attachment.FileSize, attachment.UploadedByUserId, attachment.CreatedDate);
}
