namespace TeamTaskManager.Application.Users;

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
