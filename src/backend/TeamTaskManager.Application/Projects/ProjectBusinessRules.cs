namespace TeamTaskManager.Application.Projects;

public static class ProjectBusinessRules
{
    public static IReadOnlyList<string> Validate(string name, string? description, DateOnly startDate, DateOnly targetEndDate)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length is < 3 or > 150) errors.Add("Proje adı 3 ile 150 karakter arasında olmalıdır.");
        if (description?.Length > 1000) errors.Add("Proje açıklaması en fazla 1000 karakter olabilir.");
        if (targetEndDate < startDate) errors.Add("Hedef bitiş tarihi başlangıç tarihinden önce olamaz.");
        return errors;
    }

    public static bool CanAddMember(bool userIsActive, bool alreadyActiveMember) => userIsActive && !alreadyActiveMember;
}
