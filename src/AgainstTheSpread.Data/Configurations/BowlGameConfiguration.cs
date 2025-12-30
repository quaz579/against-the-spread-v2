using AgainstTheSpread.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgainstTheSpread.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the BowlGameEntity.
/// </summary>
public class BowlGameConfiguration : IEntityTypeConfiguration<BowlGameEntity>
{
    public void Configure(EntityTypeBuilder<BowlGameEntity> builder)
    {
        builder.ToTable("BowlGames");

        // Primary key
        builder.HasKey(g => g.Id);

        // Properties
        builder.Property(g => g.Year)
            .IsRequired();

        builder.Property(g => g.GameNumber)
            .IsRequired();

        builder.Property(g => g.BowlName)
            .IsRequired()
            .HasMaxLength(200);

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
        builder.Property(g => g.SpreadWinner).HasMaxLength(100);
        builder.Property(g => g.IsPush);
        builder.Property(g => g.OutrightWinner).HasMaxLength(100);
        builder.Property(g => g.ResultEnteredAt);
        builder.Property(g => g.ResultEnteredBy);

        // Computed properties are not mapped
        builder.Ignore(g => g.IsLocked);
        builder.Ignore(g => g.HasResult);

        // Indexes
        builder.HasIndex(g => g.Year)
            .HasDatabaseName("IX_BowlGames_Year");

        builder.HasIndex(g => new { g.Year, g.GameNumber })
            .IsUnique()
            .HasDatabaseName("IX_BowlGames_Year_GameNumber");

        // Relationships
        builder.HasMany(g => g.Picks)
            .WithOne(p => p.BowlGame)
            .HasForeignKey(p => p.BowlGameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
