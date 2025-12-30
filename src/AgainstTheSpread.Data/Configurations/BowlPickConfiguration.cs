using AgainstTheSpread.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgainstTheSpread.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the BowlPickEntity.
/// </summary>
public class BowlPickConfiguration : IEntityTypeConfiguration<BowlPickEntity>
{
    public void Configure(EntityTypeBuilder<BowlPickEntity> builder)
    {
        builder.ToTable("BowlPicks");

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.BowlGameId)
            .IsRequired();

        builder.Property(p => p.SpreadPick)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.ConfidencePoints)
            .IsRequired();

        builder.Property(p => p.OutrightWinnerPick)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.SubmittedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt);

        builder.Property(p => p.Year)
            .IsRequired();

        // Indexes
        // Unique constraint: one pick per user per bowl game
        builder.HasIndex(p => new { p.UserId, p.BowlGameId })
            .IsUnique()
            .HasDatabaseName("IX_BowlPicks_UserId_BowlGameId");

        // Index for querying user's picks by year
        builder.HasIndex(p => new { p.UserId, p.Year })
            .HasDatabaseName("IX_BowlPicks_UserId_Year");

        // Index for querying user's confidence points by year (for uniqueness validation)
        builder.HasIndex(p => new { p.UserId, p.Year, p.ConfidencePoints })
            .HasDatabaseName("IX_BowlPicks_UserId_Year_Confidence");

        // Relationships
        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.BowlGame)
            .WithMany(g => g.Picks)
            .HasForeignKey(p => p.BowlGameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
