using Dealoware.Domain.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dealoware.Infrastructure.Persistence;

public class AdminSessionEntityConfiguration : IEntityTypeConfiguration<AdminSession>
{
    public void Configure(EntityTypeBuilder<AdminSession> builder)
    {
        builder.ToTable("AdminSessions");
        
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(s => s.CreatedAt)
            .IsRequired();
        
        builder.Property(s => s.LastActivityAt)
            .IsRequired();
        
        builder.Property(s => s.AbsoluteExpiresAt)
            .IsRequired();
        
        builder.Property(s => s.TotpVerified)
            .IsRequired();
        
        builder.Property(s => s.IpHmac)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.HasIndex(s => s.Email);
        builder.HasIndex(s => s.AbsoluteExpiresAt);
    }
}

public class AdminAuditEntryEntityConfiguration : IEntityTypeConfiguration<AdminAuditEntry>
{
    public void Configure(EntityTypeBuilder<AdminAuditEntry> builder)
    {
        builder.ToTable("AdminAuditLog");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Timestamp)
            .IsRequired();
        
        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(64);
        
        builder.Property(e => e.ActorEmail)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(e => e.IpHmac)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(e => e.EntityType)
            .HasMaxLength(64);
        
        builder.Property(e => e.ReasonClass)
            .HasMaxLength(64);
        
        builder.Property(e => e.BeforeSnapshot)
            .HasMaxLength(5000);
        
        builder.Property(e => e.AfterSnapshot)
            .HasMaxLength(5000);
        
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => e.Action);
        builder.HasIndex(e => e.ActorEmail);
        builder.HasIndex(e => e.EntityType);
        builder.HasIndex(e => e.CorrelationId);
    }
}

public class AdminPasswordResetTokenEntityConfiguration : IEntityTypeConfiguration<AdminPasswordResetToken>
{
    public void Configure(EntityTypeBuilder<AdminPasswordResetToken> builder)
    {
        builder.ToTable("AdminPasswordResetTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.ExpiresAt)
            .IsRequired();

        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        builder.HasIndex(t => t.Email);
        builder.HasIndex(t => t.ExpiresAt);
    }
}

public class AdminCredentialEntityConfiguration : IEntityTypeConfiguration<AdminCredential>
{
    public void Configure(EntityTypeBuilder<AdminCredential> builder)
    {
        builder.ToTable("AdminCredentials");

        builder.HasKey(c => c.Email);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(c => c.PasswordUpdatedAt)
            .IsRequired();
    }
}
