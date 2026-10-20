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

        builder.Property(a => a.FailedFactorAttempts)
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

        builder.Property(p => p.ReturnPath)
            .IsRequired()
            .HasMaxLength(2048)
            .HasDefaultValue("/admin/");

        builder.HasIndex(p => p.Email);
        builder.HasIndex(p => p.ExpiresAt);
    }
}

public class AdminAuthFailureEventEntityConfiguration : IEntityTypeConfiguration<AdminAuthFailureEvent>
{
    public void Configure(EntityTypeBuilder<AdminAuthFailureEvent> builder)
    {
        builder.ToTable("AdminAuthFailureEvents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Scope).IsRequired().HasMaxLength(32);
        builder.Property(e => e.SubjectKey).IsRequired().HasMaxLength(256);
        builder.Property(e => e.OccurredAt).IsRequired();
        builder.HasIndex(e => new { e.Scope, e.SubjectKey, e.OccurredAt });
    }
}

public class AdminAuthLockoutEntityConfiguration : IEntityTypeConfiguration<AdminAuthLockout>
{
    public void Configure(EntityTypeBuilder<AdminAuthLockout> builder)
    {
        builder.ToTable("AdminAuthLockouts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Scope).IsRequired().HasMaxLength(32);
        builder.Property(e => e.SubjectKey).IsRequired().HasMaxLength(256);
        builder.Property(e => e.StartedAt).IsRequired();
        builder.Property(e => e.ExpiresAt).IsRequired();
        builder.Property(e => e.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(0u);
        builder.HasIndex(e => new { e.Scope, e.SubjectKey }).IsUnique();
    }
}

public class AdminAuthTokenEntityConfiguration : IEntityTypeConfiguration<AdminAuthToken>
{
    public void Configure(EntityTypeBuilder<AdminAuthToken> builder)
    {
        builder.ToTable("AdminAuthTokens");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Kind).IsRequired().HasMaxLength(32);
        builder.Property(e => e.TokenHash).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(256);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.ExpiresAt).IsRequired();
        builder.Property(e => e.AttemptCount).IsRequired();
        builder.HasIndex(e => e.TokenHash).IsUnique();
        builder.HasIndex(e => new { e.Kind, e.Email });
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

        builder.Property(t => t.FailedCodeAttempts)
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

public class AdminResetIpCounterEntityConfiguration : IEntityTypeConfiguration<AdminResetIpCounter>
{
    public void Configure(EntityTypeBuilder<AdminResetIpCounter> builder)
    {
        builder.ToTable("AdminResetIpCounters");

        builder.HasKey(c => c.IpHmac);

        builder.Property(c => c.IpHmac)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(c => c.WindowStartedAt)
            .IsRequired();

        builder.Property(c => c.FailureCount)
            .IsRequired();
    }
}
