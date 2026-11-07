using TeamTaskManager.Domain.Enums;

namespace TeamTaskManager.Application.Projects;

public sealed record ProjectMemberDto(
    int Id,
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    ProjectMemberRole MemberRole,
    DateTime JoinedDate,
    bool IsActive);
