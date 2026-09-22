using Dealoware.Domain.Negotiations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// Entity configuration for AcceptGrant.
/// Stage B (#42): Persists Accept grants for contact-on-accept.
/// </summary>
public class AcceptGrantEntityConfiguration : IEntityTypeConfiguration<AcceptGrant>
{
    public void Configure(EntityTypeBuilder<AcceptGrant> builder)
    {
        builder.ToTable("AcceptGrants");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.OfferId)
            .IsRequired();

        builder.Property(g => g.NegotiationId)
            .IsRequired();

        builder.Property(g => g.GrantorSub)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(g => g.GranteeSub)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(g => g.CreatedAt)
            .IsRequired();

        builder.HasIndex(g => new { g.OfferId, g.GranteeSub })
            .IsUnique();
        
        builder.HasIndex(g => new { g.NegotiationId, g.GranteeSub });
        
        builder.HasIndex(g => g.GrantorSub);
    }
}
