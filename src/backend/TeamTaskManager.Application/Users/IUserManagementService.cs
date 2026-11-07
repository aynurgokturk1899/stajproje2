namespace TeamTaskManager.Application.Users;

public interface IUserManagementService
{
    Task<PagedResult<UserDto>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default);

    Task<UserDto?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<OperationResult<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<UserDto>> UpdateAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<UserDto>> UpdateStatusAsync(string userId, bool isActive, CancellationToken cancellationToken = default);
}
