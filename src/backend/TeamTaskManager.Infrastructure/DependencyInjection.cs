using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamTaskManager.Domain.Entities;
using TeamTaskManager.Application.Authentication;
using TeamTaskManager.Infrastructure.Authentication;
using TeamTaskManager.Infrastructure.Persistence;
using TeamTaskManager.Infrastructure.Users;
using TeamTaskManager.Application.Users;
using TeamTaskManager.Application.Projects;
using TeamTaskManager.Infrastructure.Projects;
using TeamTaskManager.Application.Tasks;
using TeamTaskManager.Infrastructure.Tasks;
using TeamTaskManager.Application.Activities;
using TeamTaskManager.Infrastructure.Activities;
using TeamTaskManager.Application.Comments;
using TeamTaskManager.Infrastructure.Comments;
using TeamTaskManager.Application.Attachments;
using TeamTaskManager.Infrastructure.Attachments;
using TeamTaskManager.Application.Notifications;
using TeamTaskManager.Infrastructure.Notifications;
using TeamTaskManager.Application.Dashboard;
using TeamTaskManager.Infrastructure.Dashboard;

namespace TeamTaskManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection yapılandırılmalıdır.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IProjectManagementService, ProjectManagementService>();
        services.AddScoped<IProjectMemberService, ProjectMemberService>();
        services.AddScoped<IProjectJoinRequestService, ProjectJoinRequestService>();
        services.AddScoped<ITaskManagementService, TaskManagementService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<ITaskCommentService, TaskCommentService>();
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddScoped<ITaskAttachmentService, TaskAttachmentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
