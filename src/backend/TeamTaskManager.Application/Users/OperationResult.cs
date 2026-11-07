namespace TeamTaskManager.Application.Users;

public sealed record OperationResult<T>(T? Data, IReadOnlyCollection<string> Errors)
{
    public bool Succeeded => Errors.Count == 0;

    public static OperationResult<T> Success(T data) => new(data, []);

    public static OperationResult<T> Failure(params string[] errors) => new(default, errors);
}
