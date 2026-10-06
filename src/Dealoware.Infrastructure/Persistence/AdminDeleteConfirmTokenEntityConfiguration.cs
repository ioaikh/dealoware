using Dealoware.Domain.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dealoware.Infrastructure.Persistence;

public sealed class AdminDeleteConfirmTokenEntityConfiguration
    : IEntityTypeConfiguration<AdminDeleteConfirmToken>
{
    public void Configure(EntityTypeBuilder<AdminDeleteConfirmToken> builder)
    {
        builder.ToTable("AdminDeleteConfirmTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        builder.Property(t => t.ActorEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.ActorSessionId)
            .IsRequired();

        builder.Property(t => t.EntityType)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(t => t.EntityId)
            .IsRequired();

        builder.Property(t => t.CascadeSetHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(t => t.ExpiresAt)
            .IsRequired();

        builder.Property(t => t.ConsumedAt);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.HasIndex(t => t.ExpiresAt);
        builder.HasIndex(t => new { t.EntityType, t.EntityId });
    }
}
