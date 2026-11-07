using TeamTaskManager.Application.Tasks;
namespace TeamTaskManager.Application.Dashboard;
public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(TaskAccessContext access, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProjectProgressDto>> GetProjectProgressAsync(TaskAccessContext access, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserWorkloadDto>> GetWorkloadAsync(TaskAccessContext access, CancellationToken cancellationToken = default);
}
