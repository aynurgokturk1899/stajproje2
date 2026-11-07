using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskManager.Api.Contracts;
using TeamTaskManager.Application.Dashboard;
using TeamTaskManager.Application.Tasks;
using TeamTaskManager.Domain.Constants;
namespace TeamTaskManager.Api.Controllers;
[ApiController, Authorize]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("api/dashboard/summary")] public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> Summary(CancellationToken ct) { var access = GetAccess(); return access is null ? Unauthorized() : Ok(ApiResponse<DashboardSummaryDto>.Ok(await dashboardService.GetSummaryAsync(access, ct), "Dashboard özeti getirildi.")); }
    [HttpGet("api/reports/project-progress")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ProjectProgressDto>>>> Progress(CancellationToken ct) { var access = GetAccess(); return access is null ? Unauthorized() : Ok(ApiResponse<IReadOnlyCollection<ProjectProgressDto>>.Ok(await dashboardService.GetProjectProgressAsync(access, ct), "Proje ilerlemeleri getirildi.")); }
    [HttpGet("api/reports/workload")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<UserWorkloadDto>>>> Workload(CancellationToken ct) { var access = GetAccess(); return access is null ? Unauthorized() : Ok(ApiResponse<IReadOnlyCollection<UserWorkloadDto>>.Ok(await dashboardService.GetWorkloadAsync(access, ct), "İş yükü getirildi.")); }
    private TaskAccessContext? GetAccess() { var id = User.FindFirstValue(ClaimTypes.NameIdentifier); return string.IsNullOrWhiteSpace(id) ? null : new TaskAccessContext(id, User.IsInRole(ApplicationRoles.Admin), User.IsInRole(ApplicationRoles.ProjectManager)); }
}
