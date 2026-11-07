namespace TeamTaskManager.Application.Tasks;

public enum TaskFailureKind { None, Validation, NotFound, Forbidden }

public sealed record TaskOperationResult<T>(T? Data, IReadOnlyCollection<string> Errors, TaskFailureKind FailureKind = TaskFailureKind.None)
{
    public bool Succeeded => Errors.Count == 0;
    public static TaskOperationResult<T> Success(T data) => new(data, []);
    public static TaskOperationResult<T> Failure(params string[] errors) => new(default, errors, TaskFailureKind.Validation);
    public static TaskOperationResult<T> NotFound(params string[] errors) => new(default, errors, TaskFailureKind.NotFound);
    public static TaskOperationResult<T> Forbidden(params string[] errors) => new(default, errors, TaskFailureKind.Forbidden);
}
