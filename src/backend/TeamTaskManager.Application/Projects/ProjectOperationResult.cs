namespace TeamTaskManager.Application.Projects;

public enum ProjectFailureKind { None, Validation, NotFound, Forbidden }

public sealed record ProjectOperationResult<T>(T? Data, IReadOnlyCollection<string> Errors, ProjectFailureKind FailureKind = ProjectFailureKind.None)
{
    public bool Succeeded => Errors.Count == 0;

    public static ProjectOperationResult<T> Success(T data) => new(data, []);

    public static ProjectOperationResult<T> Failure(params string[] errors) => new(default, errors, ProjectFailureKind.Validation);
    public static ProjectOperationResult<T> NotFound(params string[] errors) => new(default, errors, ProjectFailureKind.NotFound);
    public static ProjectOperationResult<T> Forbidden(params string[] errors) => new(default, errors, ProjectFailureKind.Forbidden);
}
