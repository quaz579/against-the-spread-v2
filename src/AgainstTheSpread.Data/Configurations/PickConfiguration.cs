using AgainstTheSpread.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgainstTheSpread.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the Pick entity.
/// </summary>
public class PickConfiguration : IEntityTypeConfiguration<Pick>
{
    public void Configure(EntityTypeBuilder<Pick> builder)
    {
        builder.ToTable("Picks");

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.GameId)
            .IsRequired();

        builder.Property(p => p.SelectedTeam)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.SubmittedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt);

        // Denormalized fields
        builder.Property(p => p.Year)
            .IsRequired();

        builder.Property(p => p.Week)
            .IsRequired();

        // Indexes
        builder.HasIndex(p => new { p.UserId, p.GameId })
            .IsUnique()
            .HasDatabaseName("IX_Picks_UserId_GameId");

        builder.HasIndex(p => new { p.UserId, p.Year, p.Week })
            .HasDatabaseName("IX_Picks_UserId_Year_Week");

        // Relationships are configured in User and Game configurations
        // but we can also specify the foreign key relationships here
        builder.HasOne(p => p.User)
            .WithMany(u => u.Picks)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Game)
            .WithMany(g => g.Picks)
            .HasForeignKey(p => p.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
