using Dealoware.Domain.Participants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dealoware.Infrastructure.Persistence;

public class ParticipantEntityConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable("Participants");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Sub)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.HasIndex(p => p.Sub)
            .IsUnique();
        
        builder.Property(p => p.DisplayName)
            .HasMaxLength(256);
        
        builder.Property(p => p.CreatedAt)
            .IsRequired();
        
        builder.Property(p => p.IsActive)
            .IsRequired();
    }
}

public class ApiKeyCredentialEntityConfiguration : IEntityTypeConfiguration<ApiKeyCredential>
{
    public void Configure(EntityTypeBuilder<ApiKeyCredential> builder)
    {
        builder.ToTable("ApiKeyCredentials");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.ParticipantId)
            .IsRequired();
        
        builder.HasIndex(c => c.ParticipantId);
        
        builder.Property(c => c.KeyPrefix)
            .IsRequired()
            .HasMaxLength(16);
        
        builder.HasIndex(c => c.KeyPrefix);
        
        builder.Property(c => c.KeyHash)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(c => c.CreatedAt)
            .IsRequired();
        
        builder.Property(c => c.RevokedAt);
    }
}

public class RevokedTokenEntityConfiguration : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> builder)
    {
        builder.ToTable("RevokedTokens");
        
        builder.HasKey(t => t.Jti);
        
        builder.Property(t => t.Jti)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(t => t.Sub)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(t => t.RevokedAt)
            .IsRequired();
        
        builder.Property(t => t.ExpiresAt)
            .IsRequired();
        
        builder.HasIndex(t => t.ExpiresAt);
    }
}
