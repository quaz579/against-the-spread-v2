using AgainstTheSpread.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgainstTheSpread.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the TeamAliasEntity.
/// </summary>
public class TeamAliasConfiguration : IEntityTypeConfiguration<TeamAliasEntity>
{
    public void Configure(EntityTypeBuilder<TeamAliasEntity> builder)
    {
        builder.ToTable("TeamAliases");

        // Primary key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.Alias)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.CanonicalName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt);

        // Indexes
        // Unique index on Alias - SQL Server is case-insensitive by default
        builder.HasIndex(t => t.Alias)
            .IsUnique()
            .HasDatabaseName("IX_TeamAliases_Alias");

        // Non-unique index on CanonicalName for lookup performance
        builder.HasIndex(t => t.CanonicalName)
            .HasDatabaseName("IX_TeamAliases_CanonicalName");
    }
}
