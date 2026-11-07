namespace TeamTaskManager.Application.Attachments;

public sealed record AttachmentDto(int Id, int TaskItemId, string OriginalFileName, string ContentType, long FileSize, string UploadedByUserId, DateTime CreatedDate);
