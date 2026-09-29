using Dealoware.Domain.Budget;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// EF Core configuration for ParticipantBudget entity.
/// Stage C #68: A8-minimum per-Participant meters.
/// </summary>
public class ParticipantBudgetEntityConfiguration : IEntityTypeConfiguration<ParticipantBudget>
{
    public void Configure(EntityTypeBuilder<ParticipantBudget> builder)
    {
        builder.ToTable("ParticipantBudgets");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.ParticipantSub)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(b => b.ParticipantSub)
            .IsUnique();

        builder.Property(b => b.LimitUnits)
            .IsRequired();

        builder.Property(b => b.UsedUnits)
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .IsRequired();
    }
}
