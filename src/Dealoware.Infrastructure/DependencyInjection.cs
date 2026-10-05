using Dealoware.Domain.AgentGateway;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Assistant;
using Dealoware.Domain.Budget;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Domain.Strategies;
using Dealoware.Infrastructure.AgentGateway;
using Dealoware.Infrastructure.Auth;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services including EF Core.
    /// Uses PostgreSQL when DB_HOST is set or the connection string is a PostgreSQL one ("Host=..."),
    /// otherwise SQLite. See <see cref="DatabaseProviderSelector"/>.
    /// Connection settings should be provided via configuration (env var or appsettings).
    /// Never commit real secrets to source.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
        => services.AddInfrastructure(connectionString, Environment.GetEnvironmentVariable, requireVerifiedTls: true);

    /// <summary>
    /// Same as <see cref="AddInfrastructure(IServiceCollection, string)"/> with an explicit
    /// environment lookup (used by tests so they do not mutate process environment).
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        Func<string, string?> getEnv)
        => services.AddInfrastructure(connectionString, getEnv, requireVerifiedTls: true);

    /// <summary>
    /// Adds infrastructure services with explicit TLS verification control.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">Connection string from configuration.</param>
    /// <param name="getEnv">Environment variable lookup function.</param>
    /// <param name="requireVerifiedTls">
    /// When true (non-Development), enforces SSL Mode=VerifyFull with the RDS root CA.
    /// When false (Development), allows weaker SSL modes for local Postgres.
    /// Pass <c>!builder.Environment.IsDevelopment()</c> from Program.cs.
    /// </param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        Func<string, string?> getEnv,
        bool requireVerifiedTls)
    {
        var selection = DatabaseProviderSelector.Select(connectionString, getEnv, requireVerifiedTls);

        services.AddDbContext<DealowareDbContext>(options =>
        {
            if (selection.Provider == DatabaseProvider.Postgres)
            {
                options.UseNpgsql(selection.ConnectionString);
            }
            else
            {
                options.UseSqlite(selection.ConnectionString);
            }
        });

        services.AddScoped<IArtifactRepository, ArtifactRepository>();
        services.AddScoped<IParticipantRepository, ParticipantRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IRevokedTokenRepository, RevokedTokenRepository>();
        services.AddScoped<INegotiationRepository, NegotiationRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IAcceptGrantRepository, AcceptGrantRepository>();
        services.AddScoped<IStrategyRepository, StrategyRepository>();
        
        services.AddSingleton<IFieldPolicy, FieldPolicy>();
        
        services.AddSingleton<IToolAllowlist, ToolAllowlist>();
        services.AddScoped<IAgentContextScrubber, AgentContextScrubber>();
        services.AddScoped<IToolExecutor, StubToolExecutor>();
        services.AddScoped<IAgentGateway, Dealoware.Domain.AgentGateway.AgentGateway>();
        
        // Stage C #66: Thin OwnAgent-only Strategy-driven AI Assistant
        // Mandatory bind to #67 gateway; OwnAgent 1:1 only; no LoginEmail
        services.AddScoped<IAssistantService, AssistantService>();
        
        // Stage C #68: Per-Participant meters + hard budgets (cutoff)
        // A8-minimum: hard cutoff fail-closed; cross-tenant isolation
        // Primary consumer: #66; metered path wall-bound (#67)
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IBudgetService, BudgetService>();

        return services;
    }

    /// <summary>
    /// Adds authentication services including JWT handling.
    /// JWT signing key should be provided via environment variable DEALOWARE_JWT_SIGNING_KEY.
    /// Never commit real secrets to source.
    /// </summary>
    public static IServiceCollection AddAuthServices(this IServiceCollection services, JwtSettings jwtSettings)
    {
        services.AddSingleton(jwtSettings);
        services.AddScoped<JwtService>();
        
        return services;
    }
}
