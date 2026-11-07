namespace TeamTaskManager.Application.Authentication;

using TeamTaskManager.Application.Projects;

public interface IAuthenticationService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<ProjectOperationResult<LoginResponse>> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken = default);

    Task<AuthenticatedUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task<ProjectOperationResult<UserProfileDto>> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);
}
