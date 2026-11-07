namespace TeamTaskManager.Application.Attachments;

public sealed record UploadAttachmentCommand(Stream Content, string OriginalFileName, string ContentType, long FileSize);
