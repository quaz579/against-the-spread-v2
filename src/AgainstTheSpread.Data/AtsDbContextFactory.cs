using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgainstTheSpread.Data;

/// <summary>
/// Design-time factory for EF Core migrations.
/// This is only used by the dotnet ef CLI tools.
/// </summary>
public class AtsDbContextFactory : IDesignTimeDbContextFactory<AtsDbContext>
{
    public AtsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AtsDbContext>();

        // Use a placeholder connection string for migrations
        // The actual connection string will be provided at runtime
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=AgainstTheSpread;Trusted_Connection=True;TrustServerCertificate=True;");

        return new AtsDbContext(optionsBuilder.Options);
    }
}
