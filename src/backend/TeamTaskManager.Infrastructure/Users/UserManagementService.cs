using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamTaskManager.Application.Users;
using TeamTaskManager.Domain.Constants;
using TeamTaskManager.Domain.Entities;

namespace TeamTaskManager.Infrastructure.Users;

public sealed class UserManagementService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager) : IUserManagementService
{
    public async Task<PagedResult<UserDto>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var users = userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            users = users.Where(user =>
                user.FirstName.Contains(search) ||
                user.LastName.Contains(search) ||
                (user.Email ?? string.Empty).Contains(search));
        }

        if (query.IsActive.HasValue)
        {
            users = users.Where(user => user.IsActive == query.IsActive.Value);
        }

        var totalCount = await users.CountAsync(cancellationToken);
        var page = await users
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var results = new List<UserDto>(page.Count);
        foreach (var user in page)
        {
            results.Add(await ToDtoAsync(user));
        }

        return new PagedResult<UserDto>(
            results,
            pageNumber,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<UserDto?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null ? null : await ToDtoAsync(user);
    }

    public async Task<OperationResult<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = await ValidateCreateRequestAsync(request);
        if (validationErrors.Count > 0)
        {
            return OperationResult<UserDto>.Failure(validationErrors.ToArray());
        }

        var user = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            UserName = request.Email.Trim(),
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return OperationResult<UserDto>.Failure(createResult.Errors.Select(error => error.Description).ToArray());
        }

        var roleResult = await userManager.AddToRolesAsync(user, GetRequestedRoles(request.Roles));
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return OperationResult<UserDto>.Failure(roleResult.Errors.Select(error => error.Description).ToArray());
        }

        return OperationResult<UserDto>.Success(await ToDtoAsync(user));
    }

    public async Task<OperationResult<UserDto>> UpdateAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return OperationResult<UserDto>.Failure("Kullanıcı bulunamadı.");
        }

        var validationErrors = await ValidateUpdateRequestAsync(userId, request);
        if (validationErrors.Count > 0)
        {
            return OperationResult<UserDto>.Failure(validationErrors.ToArray());
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = request.Email.Trim();
        user.UserName = request.Email.Trim();

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return OperationResult<UserDto>.Failure(updateResult.Errors.Select(error => error.Description).ToArray());
        }

        if (request.Roles is not null)
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                return OperationResult<UserDto>.Failure(removeResult.Errors.Select(error => error.Description).ToArray());
            }

            var addResult = await userManager.AddToRolesAsync(user, GetRequestedRoles(request.Roles));
            if (!addResult.Succeeded)
            {
                return OperationResult<UserDto>.Failure(addResult.Errors.Select(error => error.Description).ToArray());
            }
        }

        return OperationResult<UserDto>.Success(await ToDtoAsync(user));
    }

    public async Task<OperationResult<UserDto>> UpdateStatusAsync(string userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return OperationResult<UserDto>.Failure("Kullanıcı bulunamadı.");
        }

        user.IsActive = isActive;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return OperationResult<UserDto>.Failure(result.Errors.Select(error => error.Description).ToArray());
        }

        return OperationResult<UserDto>.Success(await ToDtoAsync(user));
    }

    private async Task<List<string>> ValidateCreateRequestAsync(CreateUserRequest request)
    {
        var errors = ValidateProfile(request.FirstName, request.LastName, request.Email);
        if (await userManager.FindByEmailAsync(request.Email.Trim()) is not null)
        {
            errors.Add("Bu e-posta adresi zaten kullanımda.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add("Parola zorunludur.");
        }

        errors.AddRange(await ValidateRolesAsync(request.Roles));
        return errors;
    }

    private async Task<List<string>> ValidateUpdateRequestAsync(string userId, UpdateUserRequest request)
    {
        var errors = ValidateProfile(request.FirstName, request.LastName, request.Email);
        var existingUser = await userManager.FindByEmailAsync(request.Email.Trim());
        if (existingUser is not null && existingUser.Id != userId)
        {
            errors.Add("Bu e-posta adresi zaten kullanımda.");
        }

        errors.AddRange(await ValidateRolesAsync(request.Roles));
        return errors;
    }

    private async Task<List<string>> ValidateRolesAsync(IReadOnlyCollection<string>? roles)
    {
        var errors = new List<string>();
        foreach (var role in GetRequestedRoles(roles))
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                errors.Add($"'{role}' rolü bulunamadı.");
            }
        }

        return errors;
    }

    private static List<string> ValidateProfile(string firstName, string lastName, string email)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(firstName) || firstName.Trim().Length > 80)
        {
            errors.Add("Ad zorunludur ve en fazla 80 karakter olabilir.");
        }

        if (string.IsNullOrWhiteSpace(lastName) || lastName.Trim().Length > 80)
        {
            errors.Add("Soyad zorunludur ve en fazla 80 karakter olabilir.");
        }

        if (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 256)
        {
            errors.Add("Geçerli bir e-posta adresi zorunludur.");
        }

        return errors;
    }

    private static IReadOnlyCollection<string> GetRequestedRoles(IReadOnlyCollection<string>? roles) =>
        roles is null || roles.Count == 0 ? [ApplicationRoles.TeamMember] : roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserDto(user.Id, user.FirstName, user.LastName, user.Email ?? string.Empty, user.IsActive, user.CreatedDate, roles.ToArray());
    }
}
