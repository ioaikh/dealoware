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

public class AdminCoreOwnerAccountEntityConfiguration : IEntityTypeConfiguration<AdminCoreOwnerAccount>
{
    public void Configure(EntityTypeBuilder<AdminCoreOwnerAccount> builder)
    {
        builder.ToTable("AdminCoreOwnerAccounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(a => a.Email)
            .IsUnique();

        builder.Property(a => a.PasswordHash)
            .HasMaxLength(512);

        builder.Property(a => a.TotpSecretCipher)
            .HasMaxLength(512);

        builder.Property(a => a.PendingTotpSecretCipher)
            .HasMaxLength(512);

        builder.Property(a => a.RecoveryCodesRevealCipher)
            .HasMaxLength(4000);

        builder.Property(a => a.RecoveryCodesIssued)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired();
    }
}

public class AdminRecoveryCodeEntityConfiguration : IEntityTypeConfiguration<AdminRecoveryCode>
{
    public void Configure(EntityTypeBuilder<AdminRecoveryCode> builder)
    {
        builder.ToTable("AdminRecoveryCodes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.AccountId)
            .IsRequired();

        builder.Property(c => c.CodeHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.HasIndex(c => c.AccountId);
        builder.HasIndex(c => new { c.AccountId, c.CodeHash })
            .IsUnique();
    }
}

public class AdminPendingAuthEntityConfiguration : IEntityTypeConfiguration<AdminPendingAuth>
{
    public void Configure(EntityTypeBuilder<AdminPendingAuth> builder)
    {
        builder.ToTable("AdminPendingAuths");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(p => p.TokenHash)
            .IsUnique();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.ExpiresAt)
            .IsRequired();

        builder.Property(p => p.FailedCodeAttempts)
            .IsRequired();

        builder.HasIndex(p => p.Email);
        builder.HasIndex(p => p.ExpiresAt);
    }
}
