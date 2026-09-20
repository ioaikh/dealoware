using Dealoware.Domain.Negotiations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dealoware.Infrastructure.Persistence;

public class NegotiationEntityConfiguration : IEntityTypeConfiguration<Negotiation>
{
    public void Configure(EntityTypeBuilder<Negotiation> builder)
    {
        builder.ToTable("Negotiations");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.ArtifactId)
            .IsRequired();

        builder.Property(n => n.PartyAParticipantId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(n => n.PartyBParticipantId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(n => n.PartyAIntent)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.PartyBIntent)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(n => n.StartsAt);
        builder.Property(n => n.EndsAt);
        builder.Property(n => n.CreatedAt).IsRequired();

        builder.HasMany(n => n.Offers)
            .WithOne()
            .HasForeignKey(o => o.NegotiationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => n.PartyAParticipantId);
        builder.HasIndex(n => n.PartyBParticipantId);
        builder.HasIndex(n => n.ArtifactId);
        builder.HasIndex(n => n.Status);
    }
}
