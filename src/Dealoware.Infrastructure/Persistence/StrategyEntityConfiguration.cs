using Dealoware.Domain.Strategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for Strategy entity.
/// Indexes on OwnerParticipantId for owner-scoped queries.
/// </summary>
public class StrategyEntityConfiguration : IEntityTypeConfiguration<Strategy>
{
    public void Configure(EntityTypeBuilder<Strategy> builder)
    {
        builder.ToTable("Strategies");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.OwnerParticipantId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(s => s.Name)
            .HasMaxLength(500);

        builder.Property(s => s.StrategyBody)
            .HasMaxLength(10000);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(s => s.OwnerParticipantId)
            .HasDatabaseName("IX_Strategies_OwnerParticipantId");

        builder.HasIndex(s => new { s.OwnerParticipantId, s.IsActive })
            .HasDatabaseName("IX_Strategies_OwnerParticipantId_IsActive");
    }
}
