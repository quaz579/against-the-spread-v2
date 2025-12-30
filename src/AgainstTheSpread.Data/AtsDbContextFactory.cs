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

        // Use local Docker SQL Server connection string for migrations
        // This matches the docker-compose.yml configuration
        // The actual connection string will be provided at runtime via environment variables
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=AgainstTheSpread;User Id=sa;Password=LocalDev123!;TrustServerCertificate=True;");

        return new AtsDbContext(optionsBuilder.Options);
    }
}
