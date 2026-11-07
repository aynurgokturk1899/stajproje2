namespace TeamTaskManager.Application.Attachments;

public sealed record AttachmentOperationResult<T>(T? Data, IReadOnlyCollection<string> Errors, bool IsFileTooLarge = false)
{
    public bool Succeeded => Errors.Count == 0;
    public static AttachmentOperationResult<T> Success(T data) => new(data, []);
    public static AttachmentOperationResult<T> Failure(params string[] errors) => new(default, errors);
    public static AttachmentOperationResult<T> FileTooLarge(string error) => new(default, [error], true);
}
