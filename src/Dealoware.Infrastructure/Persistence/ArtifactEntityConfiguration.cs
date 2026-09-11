using System.Text.Json;
using Dealoware.Domain.Artifacts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dealoware.Infrastructure.Persistence;

public class ArtifactEntityConfiguration : IEntityTypeConfiguration<Artifact>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<Artifact> builder)
    {
        builder.ToTable("Artifacts");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.OwnerParticipantId)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(e => e.Intent)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasMany(e => e.Entities)
            .WithOne()
            .HasForeignKey("ArtifactId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Values)
            .WithOne()
            .HasForeignKey("ArtifactId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.TimePeriods)
            .WithOne()
            .HasForeignKey("ArtifactId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property<List<string>>("_locations")
            .HasColumnName("Locations")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>(),
                new ValueComparer<List<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        builder.HasIndex(e => e.OwnerParticipantId);
    }
}

public class SubjectEntityConfiguration : IEntityTypeConfiguration<SubjectEntity>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<SubjectEntity> builder)
    {
        builder.ToTable("SubjectEntities");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(4096);
        
        builder.Property(e => e.Description)
            .HasMaxLength(4096);

        builder.HasMany(e => e.Properties)
            .WithOne()
            .HasForeignKey("SubjectEntityId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property<List<string>>("_facts")
            .HasColumnName("Facts")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>(),
                new ValueComparer<List<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));
    }
}

public class EntityPropertyConfiguration : IEntityTypeConfiguration<EntityProperty>
{
    public void Configure(EntityTypeBuilder<EntityProperty> builder)
    {
        builder.ToTable("EntityProperties");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(1024);
        
        builder.Property(e => e.Type)
            .IsRequired()
            .HasMaxLength(1024);
        
        builder.Property(e => e.Value)
            .HasMaxLength(1024);
    }
}

public class ArtifactValueConfiguration : IEntityTypeConfiguration<ArtifactValue>
{
    public void Configure(EntityTypeBuilder<ArtifactValue> builder)
    {
        builder.ToTable("ArtifactValues");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Amount)
            .HasPrecision(18, 4);
        
        builder.Property(e => e.Currency)
            .IsRequired()
            .HasMaxLength(16);
    }
}

public class TimePeriodConfiguration : IEntityTypeConfiguration<TimePeriod>
{
    public void Configure(EntityTypeBuilder<TimePeriod> builder)
    {
        builder.ToTable("TimePeriods");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Start).IsRequired();
        builder.Property(e => e.End).IsRequired();
    }
}
