namespace Dealoware.Api.Tests;

/// <summary>
/// A fact about two transactions that overlap, which only row locks settle.
/// The SQLite test store runs whole write transactions one at a time, so
/// there the test would pass without showing anything; it is reported as
/// skipped, with the reason, unless <c>DB_HOST</c> is set and the test hosts
/// are on PostgreSQL (the PostgreSQL CI job). The mirror of
/// <see cref="SqliteOnlyFactAttribute"/>.
/// </summary>
public sealed class PostgresOnlyFactAttribute : FactAttribute
{
    public PostgresOnlyFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DB_HOST")))
        {
            Skip = "Two overlapping transactions are settled by PostgreSQL row locks; DB_HOST is not set, so the test host is on SQLite.";
        }
    }
}
