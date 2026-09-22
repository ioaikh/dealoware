using Dealoware.Domain.Artifacts;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Domain.Strategies;
using Dealoware.Infrastructure.Auth;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services including EF Core with SQLite.
    /// Connection string should be provided via configuration (env var or appsettings).
    /// Never commit real secrets to source.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DealowareDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IArtifactRepository, ArtifactRepository>();
        services.AddScoped<IParticipantRepository, ParticipantRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IRevokedTokenRepository, RevokedTokenRepository>();
        services.AddScoped<INegotiationRepository, NegotiationRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IStrategyRepository, StrategyRepository>();
        
        services.AddSingleton<IFieldPolicy, FieldPolicy>();

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
