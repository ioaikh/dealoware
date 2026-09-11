using Dealoware.Domain.Artifacts;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Persistence;

public class DealowareDbContext : DbContext
{
    public DbSet<Artifact> Artifacts => Set<Artifact>();
    public DbSet<SubjectEntity> SubjectEntities => Set<SubjectEntity>();
    public DbSet<EntityProperty> EntityProperties => Set<EntityProperty>();
    public DbSet<ArtifactValue> ArtifactValues => Set<ArtifactValue>();
    public DbSet<TimePeriod> TimePeriods => Set<TimePeriod>();

    public DealowareDbContext(DbContextOptions<DealowareDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfiguration(new ArtifactEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SubjectEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EntityPropertyConfiguration());
        modelBuilder.ApplyConfiguration(new ArtifactValueConfiguration());
        modelBuilder.ApplyConfiguration(new TimePeriodConfiguration());
    }
}
