using AgainstTheSpread.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgainstTheSpread.Data;

/// <summary>
/// Entity Framework DbContext for the Against The Spread application.
/// </summary>
public class AtsDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of AtsDbContext with the specified options.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public AtsDbContext(DbContextOptions<AtsDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Users DbSet.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets or sets the Games DbSet.
    /// </summary>
    public DbSet<GameEntity> Games => Set<GameEntity>();

    /// <summary>
    /// Gets or sets the Picks DbSet.
    /// </summary>
    public DbSet<Pick> Picks => Set<Pick>();

    /// <summary>
    /// Gets or sets the BowlGames DbSet.
    /// </summary>
    public DbSet<BowlGameEntity> BowlGames => Set<BowlGameEntity>();

    /// <summary>
    /// Gets or sets the BowlPicks DbSet.
    /// </summary>
    public DbSet<BowlPickEntity> BowlPicks => Set<BowlPickEntity>();

    /// <summary>
    /// Gets or sets the TeamAliases DbSet for team name normalization.
    /// </summary>
    public DbSet<TeamAliasEntity> TeamAliases => Set<TeamAliasEntity>();

    /// <summary>
    /// Configures the model using entity configurations from this assembly.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AtsDbContext).Assembly);
    }
}
