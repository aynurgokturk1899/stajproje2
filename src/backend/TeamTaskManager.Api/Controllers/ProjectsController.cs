using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskManager.Api.Contracts;
using TeamTaskManager.Application.Projects;
using TeamTaskManager.Application.Users;
using TeamTaskManager.Domain.Constants;

namespace TeamTaskManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public sealed class ProjectsController(IProjectManagementService projectManagementService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProjectDto>>>> GetProjects(
        [FromQuery] ProjectListQuery query,
        CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null)
        {
            return Unauthorized(ApiResponse<PagedResult<ProjectDto>>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        }

        var projects = await projectManagementService.GetProjectsAsync(access, query, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProjectDto>>.Ok(projects, "Projeler getirildi."));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null)
        {
            return Unauthorized(ApiResponse<ProjectDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        }

        var result = await projectManagementService.GetByIdAsync(access, id, cancellationToken);
        if (result.Succeeded) return Ok(ApiResponse<ProjectDto>.Ok(result.Data!, "Proje getirildi."));
        var response = ApiResponse<ProjectDto>.Fail("Proje getirilemedi.", result.Errors.ToArray());
        return result.FailureKind == ProjectFailureKind.Forbidden
            ? StatusCode(StatusCodes.Status403Forbidden, response)
            : NotFound(response);
    }

    [HttpPost]
    [Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.ProjectManager)]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null)
        {
            return Unauthorized(ApiResponse<ProjectDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        }

        var result = await projectManagementService.CreateAsync(access, request, cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            return BadRequest(ApiResponse<ProjectDto>.Fail("Proje oluşturulamadı.", result.Errors.ToArray()));
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, ApiResponse<ProjectDto>.Ok(result.Data, "Proje oluşturuldu."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null)
        {
            return Unauthorized(ApiResponse<ProjectDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        }

        var result = await projectManagementService.UpdateAsync(access, id, request, cancellationToken);
        return result.Succeeded
            ? NoContent()
            : BadRequest(ApiResponse<ProjectDto>.Fail("Proje güncellenemedi.", result.Errors.ToArray()));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateProjectStatusRequest request,
        CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null)
        {
            return Unauthorized(ApiResponse<ProjectDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        }

        var result = await projectManagementService.UpdateStatusAsync(access, id, request.Status, cancellationToken);
        return result.Succeeded
            ? NoContent()
            : BadRequest(ApiResponse<ProjectDto>.Fail("Proje durumu güncellenemedi.", result.Errors.ToArray()));
    }

    private ProjectAccessContext? GetAccessContext()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId) ? null : new ProjectAccessContext(userId, User.IsInRole(ApplicationRoles.Admin));
    }
}
