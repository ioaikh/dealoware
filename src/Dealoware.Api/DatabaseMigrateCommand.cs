using Dealoware.Infrastructure;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dealoware.Api;

/// <summary>
/// One-shot migrate entrypoint: <c>dotnet Dealoware.Api.dll migrate</c>
/// (or <c>dotnet run --project src/Dealoware.Api -- migrate</c>).
/// Uses the same DB_* / connection-string path as the API. Inject
/// <c>dealoware_migrate</c> credentials for this task only — this path may
/// CREATE / INSERT <c>__EFMigrationsHistory</c> (baseline stamp + later
/// migrations). The long-lived API (<c>dealoware_app</c>) is DML-only and
/// SELECT-only on the history table and must not run this path.
/// <para>
/// After the migrations, on PostgreSQL, it applies H3 design §7 step 6b as the owner of
/// the tables (<see cref="DatabaseRoles.ApplyAppGrantsAsync"/>): the exact rights of
/// <c>dealoware_app</c> on every table, from <see cref="DatabaseTableGrants"/>. It exits 1
/// if that fails, for example when <c>dbroles</c> has not created the logins yet.
/// </para>
/// </summary>
public static class DatabaseMigrateCommand
{
    public const string CommandName = "migrate";

    public static bool IsMigrateArgs(string[] args)
        => args.Length > 0
           && string.Equals(args[0], CommandName, StringComparison.OrdinalIgnoreCase);

    public static async Task<int> RunAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DEALOWARE_CONNECTION_STRING")
            ?? "Data Source=dealoware.db";

        builder.Services.AddInfrastructure(connectionString);

        using var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();

        await DatabaseSchemaBootstrap.ApplyMigrationsAsync(db).ConfigureAwait(false);

        Console.WriteLine("Database migrations applied.");

        if (db.Database.IsNpgsql()
            && !await DatabaseRoles.ApplyAppGrantsAsync(
                (Npgsql.NpgsqlConnection)db.Database.GetDbConnection(), Console.Out).ConfigureAwait(false))
        {
            return 1;
        }

        return 0;
    }
}
