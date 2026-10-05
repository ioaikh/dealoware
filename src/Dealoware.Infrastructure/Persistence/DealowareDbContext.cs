using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Budget;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Domain.Strategies;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Infrastructure.Persistence;

public class DealowareDbContext : DbContext
{
    public DbSet<Artifact> Artifacts => Set<Artifact>();
    public DbSet<SubjectEntity> SubjectEntities => Set<SubjectEntity>();
    public DbSet<EntityProperty> EntityProperties => Set<EntityProperty>();
    public DbSet<ArtifactValue> ArtifactValues => Set<ArtifactValue>();
    public DbSet<TimePeriod> TimePeriods => Set<TimePeriod>();
    
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<ApiKeyCredential> ApiKeyCredentials => Set<ApiKeyCredential>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    public DbSet<Negotiation> Negotiations => Set<Negotiation>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<AcceptGrant> AcceptGrants => Set<AcceptGrant>();

    public DbSet<Strategy> Strategies => Set<Strategy>();

    /// <summary>
    /// Stage C #68: Per-Participant budgets for metered Assistant/LLM usage.
    /// </summary>
    public DbSet<ParticipantBudget> ParticipantBudgets => Set<ParticipantBudget>();

    public DealowareDbContext(DbContextOptions<DealowareDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Store every DateTimeOffset as UTC. PostgreSQL timestamptz (Npgsql) rejects non-zero offsets;
    /// SQLite keeps working the same way, just with normalized values.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<UtcDateTimeOffsetConverter>();
        configurationBuilder.Properties<DateTimeOffset?>()
            .HaveConversion<UtcDateTimeOffsetConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfiguration(new ArtifactEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SubjectEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EntityPropertyConfiguration());
        modelBuilder.ApplyConfiguration(new ArtifactValueConfiguration());
        modelBuilder.ApplyConfiguration(new TimePeriodConfiguration());
        
        modelBuilder.ApplyConfiguration(new ParticipantEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ApiKeyCredentialEntityConfiguration());
        modelBuilder.ApplyConfiguration(new RevokedTokenEntityConfiguration());

        modelBuilder.ApplyConfiguration(new NegotiationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OfferEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AcceptGrantEntityConfiguration());
        
        modelBuilder.ApplyConfiguration(new StrategyEntityConfiguration());
        
        // Stage C #68: Per-Participant budgets
        modelBuilder.ApplyConfiguration(new ParticipantBudgetEntityConfiguration());
    }
}
