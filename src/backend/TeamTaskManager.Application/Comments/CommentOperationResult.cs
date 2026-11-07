namespace TeamTaskManager.Application.Comments;

public sealed record CommentOperationResult<T>(T? Data, IReadOnlyCollection<string> Errors)
{
    public bool Succeeded => Errors.Count == 0;
    public static CommentOperationResult<T> Success(T data) => new(data, []);
    public static CommentOperationResult<T> Failure(params string[] errors) => new(default, errors);
}
