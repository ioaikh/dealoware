using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// Runs <see cref="SqliteDateTimeOffsetRewrite.Apply"/> when migration 20261006000200
/// emits <see cref="SqliteDateTimeOffsetRewrite.MigrationSqlSentinel"/>.
/// </summary>
public sealed class SqliteDateTimeOffsetRewriteInterceptor : DbCommandInterceptor
{
    public static readonly SqliteDateTimeOffsetRewriteInterceptor Instance = new();

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
        => RewriteIfSentinel(command) ? InterceptionResult<int>.SuppressWithResult(1) : result;

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
        => new(RewriteIfSentinel(command) ? InterceptionResult<int>.SuppressWithResult(1) : result);

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        if (!RewriteIfSentinel(command))
            return result;
        command.CommandText = "SELECT 1;";
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (RewriteIfSentinel(command))
            command.CommandText = "SELECT 1;";
        return new(result);
    }

    private static bool RewriteIfSentinel(DbCommand command)
    {
        if (command.Connection is null
            || !command.CommandText.Contains(
                SqliteDateTimeOffsetRewrite.MigrationSqlSentinel,
                StringComparison.Ordinal))
        {
            return false;
        }

        SqliteDateTimeOffsetRewrite.Apply(command.Connection);
        return true;
    }
}
