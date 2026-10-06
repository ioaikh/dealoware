using Dealoware.Domain.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dealoware.Infrastructure.Persistence;

public sealed class AdminCredentialEntityConfiguration : IEntityTypeConfiguration<AdminCredential>
{
    public void Configure(EntityTypeBuilder<AdminCredential> builder)
    {
        builder.ToTable("AdminCredentials");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(256);
        builder.HasIndex(c => c.Email)
            .IsUnique();
        builder.Property(c => c.PasswordHash)
            .HasMaxLength(512);
        builder.Property(c => c.PasswordSetAt);
    }
}

public sealed class AdminBootstrapTokenEntityConfiguration : IEntityTypeConfiguration<AdminBootstrapToken>
{
    public void Configure(EntityTypeBuilder<AdminBootstrapToken> builder)
    {
        builder.ToTable("AdminBootstrapTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64);
        builder.HasIndex(t => t.TokenHash)
            .IsUnique();
        builder.Property(t => t.CreatedAt)
            .IsRequired();
        builder.Property(t => t.ExpiresAt)
            .IsRequired();
        builder.Property(t => t.ConsumedAt);
        builder.HasIndex(t => t.ExpiresAt);
    }
}
