using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskManager.Api.Contracts;
using TeamTaskManager.Application.Tasks;
using TeamTaskManager.Application.Users;
using TeamTaskManager.Domain.Constants;

namespace TeamTaskManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(ITaskManagementService taskManagementService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<TaskItemDto>>>> GetTasks([FromQuery] TaskListQuery query, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<PagedResult<TaskItemDto>>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        var tasks = await taskManagementService.GetTasksAsync(access, query, cancellationToken);
        return Ok(ApiResponse<PagedResult<TaskItemDto>>.Ok(tasks, "Görevler getirildi."));
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<PagedResult<TaskItemDto>>>> GetMyTasks([FromQuery] TaskListQuery query, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<PagedResult<TaskItemDto>>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        var tasks = await taskManagementService.GetMyTasksAsync(access, query, cancellationToken);
        return Ok(ApiResponse<PagedResult<TaskItemDto>>.Ok(tasks, "Görevlerim getirildi."));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<TaskItemDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<TaskItemDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        var result = await taskManagementService.GetByIdAsync(access, id, cancellationToken);
        if (result.Succeeded) return Ok(ApiResponse<TaskItemDto>.Ok(result.Data!, "Görev getirildi."));
        var response = ApiResponse<TaskItemDto>.Fail("Görev getirilemedi.", result.Errors.ToArray());
        return result.FailureKind == TaskFailureKind.Forbidden
            ? StatusCode(StatusCodes.Status403Forbidden, response)
            : NotFound(response);
    }

    [Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.ProjectManager)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TaskItemDto>>> Create([FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<TaskItemDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        var result = await taskManagementService.CreateAsync(access, request, cancellationToken);
        if (!result.Succeeded || result.Data is null) return BadRequest(ApiResponse<TaskItemDto>.Fail("Görev oluşturulamadı.", result.Errors.ToArray()));
        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, ApiResponse<TaskItemDto>.Ok(result.Data, "Görev oluşturuldu."));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<TaskItemDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));

        var result = await taskManagementService.UpdateStatusAsync(access, id, request, cancellationToken);
        return MapMutationResult(result, "Görev durumu güncellendi.", "Görev durumu güncellenemedi.");
    }

    [Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.ProjectManager)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<TaskItemDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        return MapMutationResult(await taskManagementService.UpdateAsync(access, id, request, cancellationToken), "Görev güncellendi.", "Görev güncellenemedi.");
    }

    [Authorize(Roles = ApplicationRoles.Admin + "," + ApplicationRoles.ProjectManager)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<TaskItemDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        return MapMutationResult(await taskManagementService.ArchiveAsync(access, id, cancellationToken), "Görev arşivlendi.", "Görev arşivlenemedi.");
    }

    private IActionResult MapMutationResult(TaskOperationResult<TaskItemDto> result, string successMessage, string failureMessage)
    {
        if (result.Succeeded) return Ok(ApiResponse<TaskItemDto>.Ok(result.Data!, successMessage));
        var response = ApiResponse<TaskItemDto>.Fail(failureMessage, result.Errors.ToArray());
        return result.FailureKind switch
        {
            TaskFailureKind.Forbidden => StatusCode(StatusCodes.Status403Forbidden, response),
            TaskFailureKind.NotFound => NotFound(response),
            _ => BadRequest(response)
        };
    }

    private TaskAccessContext? GetAccessContext()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId) ? null : new TaskAccessContext(userId, User.IsInRole(ApplicationRoles.Admin), User.IsInRole(ApplicationRoles.ProjectManager));
    }
}
