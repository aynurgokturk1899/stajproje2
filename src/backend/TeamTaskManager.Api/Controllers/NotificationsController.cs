using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskManager.Api.Contracts;
using TeamTaskManager.Application.Notifications;
using TeamTaskManager.Application.Users;
namespace TeamTaskManager.Api.Controllers;
[ApiController, Authorize, Route("api/notifications")]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> Get(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default) { var id = User.FindFirstValue(ClaimTypes.NameIdentifier); if (string.IsNullOrWhiteSpace(id)) return Unauthorized(); return Ok(ApiResponse<PagedResult<NotificationDto>>.Ok(await notificationService.GetAsync(id, pageNumber, pageSize, cancellationToken), "Bildirimler getirildi.")); }
    [HttpPatch("{id:int}/read")] public async Task<IActionResult> Read(int id, CancellationToken cancellationToken) { var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); return string.IsNullOrWhiteSpace(userId) ? Unauthorized() : await notificationService.MarkReadAsync(userId, id, cancellationToken) ? NoContent() : NotFound(); }
    [HttpPatch("read-all")] public async Task<IActionResult> ReadAll(CancellationToken cancellationToken) { var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(); await notificationService.MarkAllReadAsync(userId, cancellationToken); return NoContent(); }
}
