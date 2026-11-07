using TeamTaskManager.Domain.Enums;

namespace TeamTaskManager.Application.Projects;

public sealed record ProjectJoinRequestDto(
    int Id, int ProjectId, string ProjectName, string UserId, string FirstName, string LastName,
    string Email, ProjectJoinRequestStatus Status, DateTime CreatedDate);
