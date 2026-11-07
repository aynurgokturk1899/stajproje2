using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskManager.Api.Contracts;
using TeamTaskManager.Application.Projects;
using TeamTaskManager.Domain.Constants;

namespace TeamTaskManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class ProjectJoinRequestsController(IProjectJoinRequestService service) : ControllerBase
{
    [HttpPost("projects/{projectId:int}/join-requests")]
    public async Task<ActionResult<ApiResponse<ProjectJoinRequestDto>>> Create(int projectId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(ApiResponse<ProjectJoinRequestDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        var result = await service.CreateAsync(userId, projectId, cancellationToken);
        return result.Succeeded && result.Data is not null
            ? StatusCode(StatusCodes.Status201Created, ApiResponse<ProjectJoinRequestDto>.Ok(result.Data, "Katılım isteği gönderildi."))
            : BadRequest(ApiResponse<ProjectJoinRequestDto>.Fail("Katılım isteği gönderilemedi.", result.Errors.ToArray()));
    }

    [HttpGet("project-join-requests/my")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ProjectJoinRequestDto>>>> My(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(ApiResponse<IReadOnlyCollection<ProjectJoinRequestDto>>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        return Ok(ApiResponse<IReadOnlyCollection<ProjectJoinRequestDto>>.Ok(await service.GetMyAsync(userId, cancellationToken), "Katılım isteklerin getirildi."));
    }

    [Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.ProjectManager)]
    [HttpGet("project-join-requests/pending")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ProjectJoinRequestDto>>>> Pending(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<ProjectJoinRequestDto>>.Ok(await service.GetPendingAsync(GetAccess(), cancellationToken), "Bekleyen katılım istekleri getirildi."));

    [Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.ProjectManager)]
    [HttpPatch("project-join-requests/{id:int}/approve")]
    public Task<ActionResult<ApiResponse<ProjectJoinRequestDto>>> Approve(int id, CancellationToken cancellationToken) => Review(id, true, cancellationToken);

    [Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.ProjectManager)]
    [HttpPatch("project-join-requests/{id:int}/reject")]
    public Task<ActionResult<ApiResponse<ProjectJoinRequestDto>>> Reject(int id, CancellationToken cancellationToken) => Review(id, false, cancellationToken);

    private async Task<ActionResult<ApiResponse<ProjectJoinRequestDto>>> Review(int id, bool approve, CancellationToken cancellationToken)
    {
        var result = await service.ReviewAsync(GetAccess(), id, approve, cancellationToken);
        return result.Succeeded && result.Data is not null ? Ok(ApiResponse<ProjectJoinRequestDto>.Ok(result.Data, "Katılım isteği güncellendi.")) : BadRequest(ApiResponse<ProjectJoinRequestDto>.Fail("Katılım isteği güncellenemedi.", result.Errors.ToArray()));
    }
    private ProjectAccessContext GetAccess() => new(User.FindFirstValue(ClaimTypes.NameIdentifier)!, User.IsInRole(ApplicationRoles.Admin));
}
