using DomainTaskStatus = TeamTaskManager.Domain.Enums.TaskStatus;

namespace TeamTaskManager.Application.Tasks;

public static class TaskBusinessRules
{
    public static IReadOnlyList<string> ValidateCreate(CreateTaskRequest request)
    {
        var errors = new List<string>();
        if (request.ProjectId <= 0) errors.Add("Geçerli proje kimliği zorunludur.");
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length is < 3 or > 200) errors.Add("Görev başlığı 3 ile 200 karakter arasında olmalıdır.");
        if (request.Description?.Length > 5000) errors.Add("Görev açıklaması en fazla 5000 karakter olabilir.");
        if (string.IsNullOrWhiteSpace(request.AssignedUserId)) errors.Add("Atanan kullanıcı zorunludur.");
        if (!Enum.IsDefined(request.Priority)) errors.Add("Geçersiz görev önceliği.");
        if (request.StartDate.HasValue && request.DueDate < request.StartDate.Value) errors.Add("Bitiş tarihi başlangıç tarihinden önce olamaz.");
        return errors;
    }

    public static IReadOnlyList<string> ValidateUpdate(UpdateTaskRequest request)
    {
        var createEquivalent = new CreateTaskRequest
        {
            ProjectId = 1,
            Title = request.Title,
            Description = request.Description,
            AssignedUserId = request.AssignedUserId,
            Priority = request.Priority,
            StartDate = request.StartDate,
            DueDate = request.DueDate
        };
        return ValidateCreate(createEquivalent);
    }

    public static bool IsTransitionAllowed(DomainTaskStatus current, DomainTaskStatus target) =>
        (current, target) switch
        {
            (DomainTaskStatus.New, DomainTaskStatus.InProgress or DomainTaskStatus.OnHold or DomainTaskStatus.Cancelled) => true,
            (DomainTaskStatus.InProgress, DomainTaskStatus.OnHold or DomainTaskStatus.Completed or DomainTaskStatus.Cancelled) => true,
            (DomainTaskStatus.OnHold, DomainTaskStatus.InProgress or DomainTaskStatus.Cancelled) => true,
            (DomainTaskStatus.Completed, DomainTaskStatus.InProgress) => true,
            (DomainTaskStatus.Cancelled, DomainTaskStatus.New) => true,
            _ => false
        };
}
