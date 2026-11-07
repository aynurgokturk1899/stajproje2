using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskManager.Api.Contracts;
using TeamTaskManager.Application.Users;
using TeamTaskManager.Domain.Constants;

namespace TeamTaskManager.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Admin)]
[Route("api/users")]
public sealed class UsersController(IUserManagementService userManagementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
        [FromQuery] UserListQuery query,
        CancellationToken cancellationToken)
    {
        var users = await userManagementService.GetUsersAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(users, "Kullanıcılar getirildi."));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(string id, CancellationToken cancellationToken)
    {
        var user = await userManagementService.GetByIdAsync(id, cancellationToken);
        return user is null
            ? NotFound(ApiResponse<UserDto>.Fail("Kullanıcı bulunamadı."))
            : Ok(ApiResponse<UserDto>.Ok(user, "Kullanıcı getirildi."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userManagementService.CreateAsync(request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(ApiResponse<UserDto>.Fail("Kullanıcı oluşturulamadı.", result.Errors.ToArray()));
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Data.Id },
            ApiResponse<UserDto>.Ok(result.Data, "Kullanıcı oluşturuldu."));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userManagementService.UpdateAsync(id, request, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<UserDto>.Fail("Kullanıcı güncellenemedi.", result.Errors.ToArray()));
        }

        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        string id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userManagementService.UpdateStatusAsync(id, request.IsActive, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse<UserDto>.Fail("Kullanıcı durumu güncellenemedi.", result.Errors.ToArray()));
        }

        return NoContent();
    }
}
