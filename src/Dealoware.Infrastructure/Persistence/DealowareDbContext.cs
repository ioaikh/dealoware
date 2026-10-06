using Dealoware.Domain.Admin;
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

    /// <summary>
    /// A7: Admin sessions for CoreOwner authentication.
    /// </summary>
    public DbSet<AdminSession> AdminSessions => Set<AdminSession>();

    /// <summary>
    /// A7: Append-only admin audit log.
    /// </summary>
    public DbSet<AdminAuditEntry> AdminAuditLog => Set<AdminAuditEntry>();

    /// <summary>
    /// A7 Step 10: single-use delete confirm tokens. Not part of the PR #23 frozen specs.
    /// </summary>
    public DbSet<AdminDeleteConfirmToken> AdminDeleteConfirmTokens => Set<AdminDeleteConfirmToken>();

    public DealowareDbContext(DbContextOptions<DealowareDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(SqliteDateTimeOffsetRewriteInterceptor.Instance);
    }

    /// <summary>
    /// Store every DateTimeOffset as UTC. PostgreSQL timestamptz (Npgsql) rejects non-zero offsets;
    /// SQLite keeps working the same way, just with normalized values.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        if (Database.IsSqlite())
        {
            configurationBuilder.Properties<DateTimeOffset>()
                .HaveConversion<SqliteSortableUtcDateTimeOffsetConverter>();
            configurationBuilder.Properties<DateTimeOffset?>()
                .HaveConversion<SqliteSortableUtcDateTimeOffsetConverter>();
        }
        else
        {
            configurationBuilder.Properties<DateTimeOffset>()
                .HaveConversion<UtcDateTimeOffsetConverter>();
            configurationBuilder.Properties<DateTimeOffset?>()
                .HaveConversion<UtcDateTimeOffsetConverter>();
        }
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
        
        // A7: Admin session and audit tables
        modelBuilder.ApplyConfiguration(new AdminSessionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AdminAuditEntryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AdminDeleteConfirmTokenEntityConfiguration());
    }
}
