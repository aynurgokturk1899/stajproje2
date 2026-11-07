using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskManager.Api.Contracts;
using TeamTaskManager.Application.Comments;
using TeamTaskManager.Application.Tasks;
using TeamTaskManager.Domain.Constants;

namespace TeamTaskManager.Api.Controllers;

[ApiController]
[Authorize]
public sealed class TaskCommentsController(ITaskCommentService taskCommentService) : ControllerBase
{
    [HttpGet("api/tasks/{taskId:int}/comments")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TaskCommentDto>>>> GetComments(int taskId, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<IReadOnlyCollection<TaskCommentDto>>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        var comments = await taskCommentService.GetCommentsAsync(access, taskId, cancellationToken);
        return comments is null ? NotFound(ApiResponse<IReadOnlyCollection<TaskCommentDto>>.Fail("Görev bulunamadı veya yorumları görme yetkiniz yok.")) : Ok(ApiResponse<IReadOnlyCollection<TaskCommentDto>>.Ok(comments, "Yorumlar getirildi."));
    }

    [HttpPost("api/tasks/{taskId:int}/comments")]
    public async Task<ActionResult<ApiResponse<TaskCommentDto>>> Create(int taskId, [FromBody] CreateTaskCommentRequest request, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<TaskCommentDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        var result = await taskCommentService.CreateAsync(access, taskId, request, cancellationToken);
        if (!result.Succeeded || result.Data is null) return BadRequest(ApiResponse<TaskCommentDto>.Fail("Yorum eklenemedi.", result.Errors.ToArray()));
        return CreatedAtAction(nameof(GetComments), new { taskId }, ApiResponse<TaskCommentDto>.Ok(result.Data, "Yorum eklendi."));
    }

    [HttpPut("api/comments/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskCommentRequest request, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<TaskCommentDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        var result = await taskCommentService.UpdateAsync(access, id, request, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(ApiResponse<TaskCommentDto>.Fail("Yorum güncellenemedi.", result.Errors.ToArray()));
    }

    [HttpDelete("api/comments/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<TaskCommentDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        var result = await taskCommentService.DeleteAsync(access, id, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(ApiResponse<TaskCommentDto>.Fail("Yorum silinemedi.", result.Errors.ToArray()));
    }

    private TaskAccessContext? GetAccessContext()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId) ? null : new TaskAccessContext(userId, User.IsInRole(ApplicationRoles.Admin), User.IsInRole(ApplicationRoles.ProjectManager));
    }
}
