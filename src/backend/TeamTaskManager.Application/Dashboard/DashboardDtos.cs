namespace TeamTaskManager.Application.Dashboard;
public sealed record DashboardSummaryDto(int ActiveProjectCount, int OpenTaskCount, int MyOpenTaskCount, int UnreadNotificationCount);
public sealed record ProjectProgressDto(int ProjectId, string ProjectName, string Status, int TotalTaskCount, int CompletedTaskCount, decimal CompletionPercentage);
public sealed record UserWorkloadDto(string UserId, string FirstName, string LastName, string Email, int OpenTaskCount, int TotalTaskCount);
