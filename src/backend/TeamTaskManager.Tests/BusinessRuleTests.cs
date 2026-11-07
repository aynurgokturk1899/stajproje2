using TeamTaskManager.Application.Projects;
using TeamTaskManager.Application.Tasks;
using TeamTaskManager.Domain.Enums;
using DomainTaskStatus = TeamTaskManager.Domain.Enums.TaskStatus;

namespace TeamTaskManager.Tests;

public sealed class TaskServiceBusinessRuleTests
{
    private static CreateTaskRequest ValidTask() => new()
    {
        ProjectId = 1, Title = "API testlerini yaz", AssignedUserId = "member-1",
        Priority = TaskPriority.High, StartDate = new DateOnly(2026, 9, 15), DueDate = new DateOnly(2026, 9, 20)
    };

    [Fact] public void Create_ValidRequest_Succeeds() => Assert.Empty(TaskBusinessRules.ValidateCreate(ValidTask()));

    [Fact]
    public void Create_DueDateBeforeStart_ReturnsError()
    {
        var task = new CreateTaskRequest { ProjectId = 1, Title = "API testlerini yaz", AssignedUserId = "member-1", Priority = TaskPriority.High, StartDate = new DateOnly(2026, 9, 20), DueDate = new DateOnly(2026, 9, 15) };
        Assert.Contains(TaskBusinessRules.ValidateCreate(task), error => error.Contains("başlangıç"));
    }

    [Fact] public void Status_NewToCompleted_IsRejected() => Assert.False(TaskBusinessRules.IsTransitionAllowed(DomainTaskStatus.New, DomainTaskStatus.Completed));
    [Fact] public void Status_InProgressToCompleted_IsAllowed() => Assert.True(TaskBusinessRules.IsTransitionAllowed(DomainTaskStatus.InProgress, DomainTaskStatus.Completed));
    [Fact] public void Status_CompletedCanBeReopened() => Assert.True(TaskBusinessRules.IsTransitionAllowed(DomainTaskStatus.Completed, DomainTaskStatus.InProgress));

    [Fact]
    public void Update_AssigneeMissing_ReturnsValidationError()
    {
        var request = new UpdateTaskRequest { Title = "Geçerli başlık", AssignedUserId = "", Priority = TaskPriority.Medium, DueDate = new DateOnly(2026, 9, 20) };
        Assert.Contains(TaskBusinessRules.ValidateUpdate(request), error => error.Contains("Atanan kullanıcı"));
    }

    [Fact]
    public void Update_EndBeforeStart_ReturnsValidationError()
    {
        var request = new UpdateTaskRequest { Title = "Geçerli başlık", AssignedUserId = "member-1", Priority = TaskPriority.Medium, StartDate = new DateOnly(2026, 9, 21), DueDate = new DateOnly(2026, 9, 20) };
        Assert.Contains(TaskBusinessRules.ValidateUpdate(request), error => error.Contains("başlangıç"));
    }

    [Fact] public void ForbiddenResult_ReportsForbiddenKind() => Assert.Equal(TaskFailureKind.Forbidden, TaskOperationResult<TaskItemDto>.Forbidden("Yetki yok.").FailureKind);
}

public sealed class ProjectServiceBusinessRuleTests
{
    [Fact] public void Create_ValidProject_Succeeds() => Assert.Empty(ProjectBusinessRules.Validate("Yeni Portal", null, new DateOnly(2026, 9, 15), new DateOnly(2026, 10, 15)));
    [Fact] public void Create_EndBeforeStart_ReturnsError() => Assert.Contains(ProjectBusinessRules.Validate("Yeni Portal", null, new DateOnly(2026, 10, 15), new DateOnly(2026, 9, 15)), error => error.Contains("başlangıç"));
    [Fact] public void AddMember_ActiveExistingMember_IsRejected() => Assert.False(ProjectBusinessRules.CanAddMember(userIsActive: true, alreadyActiveMember: true));
}
