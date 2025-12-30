using AgainstTheSpread.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgainstTheSpread.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the GameEntity.
/// </summary>
public class GameConfiguration : IEntityTypeConfiguration<GameEntity>
{
    public void Configure(EntityTypeBuilder<GameEntity> builder)
    {
        builder.ToTable("Games");

        // Primary key
        builder.HasKey(g => g.Id);

        // Properties
        builder.Property(g => g.Year)
            .IsRequired();

        builder.Property(g => g.Week)
            .IsRequired();

        builder.Property(g => g.Favorite)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(g => g.Underdog)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(g => g.Line)
            .IsRequired()
            .HasPrecision(5, 1); // e.g., -21.5

        builder.Property(g => g.GameDate)
            .IsRequired();

        // Result fields - all nullable
        builder.Property(g => g.FavoriteScore);

        builder.Property(g => g.UnderdogScore);

        builder.Property(g => g.SpreadWinner)
            .HasMaxLength(100);

        builder.Property(g => g.IsPush);

        builder.Property(g => g.ResultEnteredAt);

        builder.Property(g => g.ResultEnteredBy);

        // Computed properties are not mapped (using [NotMapped] attribute)
        builder.Ignore(g => g.IsLocked);
        builder.Ignore(g => g.HasResult);

        // Indexes
        builder.HasIndex(g => new { g.Year, g.Week })
            .HasDatabaseName("IX_Games_Year_Week");

        builder.HasIndex(g => new { g.Year, g.Week, g.Favorite, g.Underdog })
            .IsUnique()
            .HasDatabaseName("IX_Games_Year_Week_Favorite_Underdog");

        // Relationships
        builder.HasMany(g => g.Picks)
            .WithOne(p => p.Game)
            .HasForeignKey(p => p.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
