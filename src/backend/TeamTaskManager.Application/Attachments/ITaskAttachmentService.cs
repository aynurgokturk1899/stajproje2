using TeamTaskManager.Application.Tasks;

namespace TeamTaskManager.Application.Attachments;

public interface ITaskAttachmentService
{
    Task<IReadOnlyCollection<AttachmentDto>?> GetForTaskAsync(TaskAccessContext access, int taskId, CancellationToken cancellationToken = default);
    Task<AttachmentOperationResult<AttachmentDto>> UploadAsync(TaskAccessContext access, int taskId, UploadAttachmentCommand command, CancellationToken cancellationToken = default);
    Task<AttachmentOperationResult<AttachmentDownload>> DownloadAsync(TaskAccessContext access, int attachmentId, CancellationToken cancellationToken = default);
    Task<AttachmentOperationResult<AttachmentDto>> DeleteAsync(TaskAccessContext access, int attachmentId, CancellationToken cancellationToken = default);
}
