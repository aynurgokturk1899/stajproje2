using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TeamTaskManager.Application.Authentication;
using TeamTaskManager.Application.Projects;
using TeamTaskManager.Domain.Constants;
using TeamTaskManager.Domain.Entities;
using TeamTaskManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DomainTaskStatus = TeamTaskManager.Domain.Enums.TaskStatus;

namespace TeamTaskManager.Infrastructure.Authentication;

public sealed class AuthenticationService(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> jwtOptions,
    ApplicationDbContext dbContext) : IAuthenticationService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
        var authenticatedUser = ToAuthenticatedUser(user, roles);

        return new LoginResponse(CreateAccessToken(authenticatedUser, expiresAt), expiresAt, authenticatedUser);
    }

    public async Task<AuthenticatedUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return ToAuthenticatedUser(user, roles);
    }

    public async Task<UserProfileDto?> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive) return null;
        return await ToProfileAsync(user, cancellationToken);
    }

    public async Task<ProjectOperationResult<UserProfileDto>> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive) return ProjectOperationResult<UserProfileDto>.Failure("Kullanıcı bulunamadı veya pasif durumda.");
        user.FirstName = request.FirstName.Trim(); user.LastName = request.LastName.Trim();
        user.PhoneNumber = Normalize(request.PhoneNumber);
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded ? ProjectOperationResult<UserProfileDto>.Success(await ToProfileAsync(user, cancellationToken)) : ProjectOperationResult<UserProfileDto>.Failure(result.Errors.Select(x => x.Description).ToArray());
    }

    private async Task<UserProfileDto> ToProfileAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var projects = dbContext.Projects.Where(p => !p.IsArchived && (p.ManagerUserId == user.Id || p.Members.Any(m => m.UserId == user.Id && m.IsActive)));
        var tasks = dbContext.TaskItems.Where(t => !t.IsArchived && t.AssignedUserId == user.Id);
        var activityRows = await dbContext.ActivityLogs.AsNoTracking()
            .Where(a => a.UserId == user.Id)
            .OrderByDescending(a => a.CreatedDate)
            .Take(8)
            .ToListAsync(cancellationToken);
        var projectIds = activityRows.Where(a => a.EntityType == "Project").Select(a => a.EntityId).Distinct().ToArray();
        var taskIds = activityRows.Where(a => a.EntityType == "TaskItem").Select(a => a.EntityId).Distinct().ToArray();
        var projectNames = await dbContext.Projects.AsNoTracking().Where(p => projectIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);
        var taskNames = await dbContext.TaskItems.AsNoTracking().Where(t => taskIds.Contains(t.Id)).Select(t => new { t.Id, t.Title, ProjectName = t.Project.Name }).ToDictionaryAsync(t => t.Id, cancellationToken);
        var activities = activityRows.Select(a =>
        {
            if (a.EntityType == "Project" && projectNames.TryGetValue(a.EntityId, out var projectName))
                return new ProfileActivityDto(a.Id, a.Action, a.Description, a.EntityType, a.EntityId, projectName, projectName, a.CreatedDate);
            if (a.EntityType == "TaskItem" && taskNames.TryGetValue(a.EntityId, out var task))
                return new ProfileActivityDto(a.Id, a.Action, a.Description, a.EntityType, a.EntityId, task.Title, task.ProjectName, a.CreatedDate);
            return new ProfileActivityDto(a.Id, a.Action, a.Description, a.EntityType, a.EntityId, null, null, a.CreatedDate);
        }).ToArray();
        return new UserProfileDto(user.Id, user.FirstName, user.LastName, user.Email ?? "", user.PhoneNumber, roles.ToArray(), user.IsActive, user.CreatedDate, await projects.CountAsync(cancellationToken), await tasks.CountAsync(t => t.Status != DomainTaskStatus.Completed && t.Status != DomainTaskStatus.Cancelled, cancellationToken), await tasks.CountAsync(t => t.Status == DomainTaskStatus.Completed, cancellationToken), await tasks.CountAsync(t => t.Status != DomainTaskStatus.Completed && t.Status != DomainTaskStatus.Cancelled && t.DueDate < DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken), activities);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task<ProjectOperationResult<LoginResponse>> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken = default)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return ProjectOperationResult<LoginResponse>.Failure("Bu e-posta adresiyle daha önce kullanıcı hesabı oluşturulmuş.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };
        var creationResult = await userManager.CreateAsync(user, request.Password);
        if (!creationResult.Succeeded)
        {
            return ProjectOperationResult<LoginResponse>.Failure(creationResult.Errors.Select(error => error.Description).ToArray());
        }

        var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.TeamMember);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return ProjectOperationResult<LoginResponse>.Failure(roleResult.Errors.Select(error => error.Description).ToArray());
        }

        var roles = await userManager.GetRolesAsync(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
        var authenticatedUser = ToAuthenticatedUser(user, roles);
        return ProjectOperationResult<LoginResponse>.Success(new LoginResponse(CreateAccessToken(authenticatedUser, expiresAt), expiresAt, authenticatedUser));
    }

    public async Task<IReadOnlyCollection<string>> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            return ["Kullanıcı bulunamadı veya pasif durumda."];
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        return result.Succeeded ? [] : result.Errors.Select(error => error.Description).ToArray();
    }

    private string CreateAccessToken(AuthenticatedUser user, DateTime expiresAt)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName)
        };

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static AuthenticatedUser ToAuthenticatedUser(ApplicationUser user, IEnumerable<string> roles) =>
        new(user.Id, user.FirstName, user.LastName, user.Email ?? string.Empty, roles.ToArray(), user.IsActive);
}
