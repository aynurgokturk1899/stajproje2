using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskManager.Api.Contracts;
using TeamTaskManager.Application.Attachments;
using TeamTaskManager.Application.Tasks;
using TeamTaskManager.Domain.Constants;

namespace TeamTaskManager.Api.Controllers;

[ApiController]
[Authorize]
public sealed class TaskAttachmentsController(ITaskAttachmentService taskAttachmentService) : ControllerBase
{
    [HttpGet("api/tasks/{taskId:int}/attachments")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AttachmentDto>>>> GetForTask(int taskId, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<IReadOnlyCollection<AttachmentDto>>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        var attachments = await taskAttachmentService.GetForTaskAsync(access, taskId, cancellationToken);
        return attachments is null
            ? NotFound(ApiResponse<IReadOnlyCollection<AttachmentDto>>.Fail("Görev bulunamadı veya dosyaları görme yetkiniz yok."))
            : Ok(ApiResponse<IReadOnlyCollection<AttachmentDto>>.Ok(attachments, "Görev dosyaları getirildi."));
    }

    [HttpPost("api/tasks/{taskId:int}/attachments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_242_880)]
    public async Task<ActionResult<ApiResponse<AttachmentDto>>> Upload(int taskId, IFormFile file, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<AttachmentDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        if (file is null) return BadRequest(ApiResponse<AttachmentDto>.Fail("Yüklenecek dosya zorunludur."));

        await using var stream = file.OpenReadStream();
        var result = await taskAttachmentService.UploadAsync(access, taskId, new UploadAttachmentCommand(stream, file.FileName, file.ContentType, file.Length), cancellationToken);
        if (result.Succeeded && result.Data is not null) return Created($"/api/attachments/{result.Data.Id}/download", ApiResponse<AttachmentDto>.Ok(result.Data, "Dosya yüklendi."));
        return result.IsFileTooLarge ? StatusCode(StatusCodes.Status413PayloadTooLarge, ApiResponse<AttachmentDto>.Fail("Dosya çok büyük.", result.Errors.ToArray())) : BadRequest(ApiResponse<AttachmentDto>.Fail("Dosya yüklenemedi.", result.Errors.ToArray()));
    }

    [HttpGet("api/attachments/{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<AttachmentDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        var result = await taskAttachmentService.DownloadAsync(access, id, cancellationToken);
        return !result.Succeeded || result.Data is null ? NotFound(ApiResponse<AttachmentDto>.Fail("Dosya bulunamadı.")) : File(result.Data.Content, result.Data.ContentType, result.Data.OriginalFileName);
    }

    [HttpDelete("api/attachments/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var access = GetAccessContext();
        if (access is null) return Unauthorized(ApiResponse<AttachmentDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        var result = await taskAttachmentService.DeleteAsync(access, id, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(ApiResponse<AttachmentDto>.Fail("Dosya silinemedi.", result.Errors.ToArray()));
    }

    private TaskAccessContext? GetAccessContext()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId) ? null : new TaskAccessContext(userId, User.IsInRole(ApplicationRoles.Admin), User.IsInRole(ApplicationRoles.ProjectManager));
    }
}
