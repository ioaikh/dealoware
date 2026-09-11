using Dealoware.Domain.Artifacts;
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

        return services;
    }
}
