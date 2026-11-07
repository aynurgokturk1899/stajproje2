using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskManager.Api.Contracts;
using TeamTaskManager.Application.Projects;
using TeamTaskManager.Application.Users;
using TeamTaskManager.Domain.Constants;

namespace TeamTaskManager.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.ProjectManager)]
[Route("api/projects/{projectId:int}/members")]
public sealed class ProjectMembersController(IProjectMemberService projectMemberService) : ControllerBase
{
    [HttpGet("eligible-users")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<UserDto>>>> GetEligibleUsers(
        int projectId,
        CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<IReadOnlyCollection<UserDto>>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        var users = await projectMemberService.GetEligibleUsersAsync(access, projectId, cancellationToken);
        return users is null
            ? StatusCode(StatusCodes.Status403Forbidden, ApiResponse<IReadOnlyCollection<UserDto>>.Fail("Bu projenin üyelerini yönetme yetkiniz yok."))
            : Ok(ApiResponse<IReadOnlyCollection<UserDto>>.Ok(users, "Projeye eklenebilecek kullanıcılar getirildi."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ProjectMemberDto>>>> GetMembers(
        int projectId,
        CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null)
        {
            return Unauthorized(ApiResponse<IReadOnlyCollection<ProjectMemberDto>>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        }

        var members = await projectMemberService.GetMembersAsync(access, projectId, cancellationToken);
        return members is null
            ? NotFound(ApiResponse<IReadOnlyCollection<ProjectMemberDto>>.Fail("Proje bulunamadı veya üye listesini görme yetkiniz yok."))
            : Ok(ApiResponse<IReadOnlyCollection<ProjectMemberDto>>.Ok(members, "Proje üyeleri getirildi."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProjectMemberDto>>> AddMember(
        int projectId,
        [FromBody] AddProjectMemberRequest request,
        CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null)
        {
            return Unauthorized(ApiResponse<ProjectMemberDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        }

        var result = await projectMemberService.AddAsync(access, projectId, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(ApiResponse<ProjectMemberDto>.Fail("Proje üyesi eklenemedi.", result.Errors.ToArray()));
        }

        return CreatedAtAction(nameof(GetMembers), new { projectId }, ApiResponse<ProjectMemberDto>.Ok(result.Data, "Proje üyesi eklendi."));
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> RemoveMember(int projectId, string userId, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null)
        {
            return Unauthorized(ApiResponse<ProjectMemberDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        }

        var result = await projectMemberService.RemoveAsync(access, projectId, userId, cancellationToken);
        return result.Succeeded
            ? NoContent()
            : BadRequest(ApiResponse<ProjectMemberDto>.Fail("Proje üyesi çıkarılamadı.", result.Errors.ToArray()));
    }

    private ProjectAccessContext? GetAccessContext()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId) ? null : new ProjectAccessContext(userId, User.IsInRole(ApplicationRoles.Admin));
    }
}
