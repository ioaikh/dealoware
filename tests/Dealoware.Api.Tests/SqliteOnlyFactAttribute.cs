namespace Dealoware.Api.Tests;

/// <summary>
/// A fact that drives the database through SQLite-only SQL (for example a
/// trigger that forces a fault). The test hosts move to PostgreSQL exactly
/// when <c>DB_HOST</c> is set, so there the test is reported as skipped,
/// with the reason, instead of passing without running.
/// </summary>
public sealed class SqliteOnlyFactAttribute : FactAttribute
{
    public SqliteOnlyFactAttribute()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DB_HOST")))
        {
            Skip = "Forces the database fault with a SQLite trigger; DB_HOST is set, so the test host is on PostgreSQL.";
        }
    }
}
