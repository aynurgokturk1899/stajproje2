using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TeamTaskManager.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=TeamTaskManagerDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
