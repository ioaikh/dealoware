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
        builder.HasIndex(e => new { e.Scope, e.SubjectKey, e.ExpiresAt });
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
