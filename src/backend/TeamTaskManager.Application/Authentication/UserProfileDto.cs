namespace TeamTaskManager.Application.Authentication;

public sealed record UserProfileDto(
    string Id, string FirstName, string LastName, string Email, string? PhoneNumber,
    IReadOnlyCollection<string> Roles, bool IsActive, DateTime CreatedDate,
    int ProjectCount, int OpenTaskCount, int CompletedTaskCount,
    int OverdueTaskCount, IReadOnlyCollection<ProfileActivityDto> RecentActivities);

public sealed record ProfileActivityDto(
    long Id,
    string Action,
    string Description,
    string EntityType,
    int EntityId,
    string? EntityName,
    string? ProjectName,
    DateTime CreatedDate);
