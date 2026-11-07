using System.ComponentModel.DataAnnotations;
using TeamTaskManager.Application.Authentication;
using TeamTaskManager.Application.Projects;
using TeamTaskManager.Application.Tasks;
using TeamTaskManager.Domain.Enums;

namespace TeamTaskManager.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void LoginRequest_EmailYoksa_GecersizOlur()
    {
        var errors = Validate(new LoginRequest { Email = string.Empty, Password = "guclu-sifre" });

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(LoginRequest.Email)));
    }

    [Fact]
    public void CreateProjectRequest_ZorunluAlanlarDoluysa_GecerliOlur()
    {
        var request = new CreateProjectRequest
        {
            Name = "Mobil Uygulama",
            Description = "Takım görevlerini yönetir",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            TargetEndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30))
        };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void CreateTaskRequest_BaslikYoksa_GecersizOlur()
    {
        var request = new CreateTaskRequest
        {
            ProjectId = 1,
            Title = string.Empty,
            AssignedUserId = "kullanici-1",
            Priority = TaskPriority.High,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7))
        };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(CreateTaskRequest.Title)));
    }

    private static IReadOnlyCollection<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
