using Microsoft.EntityFrameworkCore;
using TeamTaskManager.Application.Dashboard;
using TeamTaskManager.Application.Tasks;
using TeamTaskManager.Domain.Enums;
using TeamTaskManager.Infrastructure.Persistence;
using TaskStatus = TeamTaskManager.Domain.Enums.TaskStatus;
namespace TeamTaskManager.Infrastructure.Dashboard;
public sealed class DashboardService(ApplicationDbContext dbContext) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(TaskAccessContext access, CancellationToken cancellationToken = default)
    {
        var projects = AccessibleProjects(access);
        var tasks = AccessibleTasks(access);
        var open = tasks.Where(t => t.Status != TaskStatus.Completed && t.Status != TaskStatus.Cancelled);
        return new DashboardSummaryDto(await projects.CountAsync(cancellationToken), await open.CountAsync(cancellationToken), await open.CountAsync(t => t.AssignedUserId == access.UserId, cancellationToken), await dbContext.Notifications.CountAsync(n => n.UserId == access.UserId && !n.IsRead, cancellationToken));
    }
    public async Task<IReadOnlyCollection<ProjectProgressDto>> GetProjectProgressAsync(TaskAccessContext access, CancellationToken cancellationToken = default) =>
        await AccessibleProjects(access).OrderBy(p => p.Name).Select(p => new ProjectProgressDto(p.Id, p.Name, p.Status.ToString(), p.Tasks.Count(t => !t.IsArchived), p.Tasks.Count(t => !t.IsArchived && t.Status == TaskStatus.Completed), p.Tasks.Count(t => !t.IsArchived) == 0 ? 0 : Math.Round((decimal)p.Tasks.Count(t => !t.IsArchived && t.Status == TaskStatus.Completed) * 100 / p.Tasks.Count(t => !t.IsArchived), 2))).ToListAsync(cancellationToken);
    public async Task<IReadOnlyCollection<UserWorkloadDto>> GetWorkloadAsync(TaskAccessContext access, CancellationToken cancellationToken = default)
    {
        var tasks = AccessibleTasks(access);
        if (!access.IsAdmin && !access.IsProjectManager) tasks = tasks.Where(t => t.AssignedUserId == access.UserId);
        return await (from task in tasks join user in dbContext.Users on task.AssignedUserId equals user.Id group task by new { user.Id, user.FirstName, user.LastName, user.Email } into groupTasks orderby groupTasks.Count() descending select new UserWorkloadDto(groupTasks.Key.Id, groupTasks.Key.FirstName, groupTasks.Key.LastName, groupTasks.Key.Email ?? string.Empty, groupTasks.Count(t => t.Status != TaskStatus.Completed && t.Status != TaskStatus.Cancelled), groupTasks.Count())).ToListAsync(cancellationToken);
    }
    private IQueryable<TeamTaskManager.Domain.Entities.Project> AccessibleProjects(TaskAccessContext access) { var projects = dbContext.Projects.AsNoTracking().Where(p => !p.IsArchived); return access.IsAdmin ? projects : projects.Where(p => p.ManagerUserId == access.UserId || p.Members.Any(m => m.UserId == access.UserId && m.IsActive)); }
    private IQueryable<TeamTaskManager.Domain.Entities.TaskItem> AccessibleTasks(TaskAccessContext access) { var tasks = dbContext.TaskItems.AsNoTracking().Where(t => !t.IsArchived); return access.IsAdmin ? tasks : tasks.Where(t => t.Project.ManagerUserId == access.UserId || t.Project.Members.Any(m => m.UserId == access.UserId && m.IsActive)); }
}
