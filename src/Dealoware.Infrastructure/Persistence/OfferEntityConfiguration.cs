using Dealoware.Domain.Negotiations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dealoware.Infrastructure.Persistence;

public class OfferEntityConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("Offers");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.NegotiationId)
            .IsRequired();

        builder.Property(o => o.FromParticipantId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.ToParticipantId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(o => o.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.Currency)
            .HasMaxLength(3);

        builder.Property(o => o.Terms)
            .HasMaxLength(2000);

        builder.Property(o => o.CreatedAt).IsRequired();

        builder.HasIndex(o => o.NegotiationId);
        builder.HasIndex(o => o.FromParticipantId);
        builder.HasIndex(o => o.ToParticipantId);
        builder.HasIndex(o => o.Status);
    }
}
