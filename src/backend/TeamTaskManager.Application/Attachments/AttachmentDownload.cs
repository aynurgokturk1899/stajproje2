namespace TeamTaskManager.Application.Attachments;

public sealed record AttachmentDownload(Stream Content, string OriginalFileName, string ContentType);
