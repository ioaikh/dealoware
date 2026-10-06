using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// One-shot SQLite rewrite of DateTimeOffset TEXT from the legacy EF store format
/// to <see cref="SqliteSortableUtcDateTimeOffsetConverter.Format"/>. Idempotent.
/// PostgreSQL is a no-op (timestamptz, not TEXT).
/// </summary>
public static class SqliteDateTimeOffsetRewrite
{
    public const string SqliteProviderName = "Microsoft.EntityFrameworkCore.Sqlite";

    /// <summary>
    /// Sentinel queued by migration 20261006000200. The interceptor runs
    /// <see cref="Apply"/> on that connection, then replaces the command with SELECT 1.
    /// </summary>
    public const string MigrationSqlSentinel = "-- Dealoware_SqliteDateTimeOffsetRewrite";

    /// <summary>
    /// Every table/column mapped to DateTimeOffset or DateTimeOffset? on
    /// <see cref="DealowareDbContext"/>. Keep in lockstep with the model;
    /// <c>TD_ADM_066_SqliteRewrite_ColumnsMatchModel</c> asserts equality.
    /// </summary>
    public static readonly IReadOnlyList<(string Table, string Column)> Columns =
    [
        ("AcceptGrants", "CreatedAt"),
        ("AdminAuditLog", "Timestamp"),
        ("AdminDeleteConfirmTokens", "ConsumedAt"),
        ("AdminDeleteConfirmTokens", "CreatedAt"),
        ("AdminDeleteConfirmTokens", "ExpiresAt"),
        ("AdminSessions", "AbsoluteExpiresAt"),
        ("AdminSessions", "CreatedAt"),
        ("AdminSessions", "LastActivityAt"),
        ("ApiKeyCredentials", "CreatedAt"),
        ("ApiKeyCredentials", "RevokedAt"),
        ("Artifacts", "CreatedAt"),
        ("Artifacts", "DeletedAt"),
        ("Artifacts", "UpdatedAt"),
        ("Negotiations", "CreatedAt"),
        ("Negotiations", "DeletedAt"),
        ("Negotiations", "EndsAt"),
        ("Negotiations", "StartsAt"),
        ("Negotiations", "UpdatedAt"),
        ("Offers", "CreatedAt"),
        ("Offers", "DeletedAt"),
        ("Offers", "UpdatedAt"),
        ("ParticipantBudgets", "CreatedAt"),
        ("ParticipantBudgets", "UpdatedAt"),
        ("Participants", "CreatedAt"),
        ("Participants", "DeletedAt"),
        ("Participants", "UpdatedAt"),
        ("RevokedTokens", "ExpiresAt"),
        ("RevokedTokens", "RevokedAt"),
        ("Strategies", "CreatedAt"),
        ("Strategies", "UpdatedAt"),
        ("TimePeriods", "End"),
        ("TimePeriods", "Start")
    ];

    public static bool IsSqliteProvider(string? provider)
        => string.Equals(provider, SqliteProviderName, StringComparison.Ordinal);

    public static IReadOnlyList<(string Table, string Column)> DiscoverFrom(IModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var found = new List<(string Table, string Column)>();
        foreach (var entity in model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (string.IsNullOrEmpty(table))
                continue;
            var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType != typeof(DateTimeOffset) && property.ClrType != typeof(DateTimeOffset?))
                    continue;
                var column = property.GetColumnName(store);
                if (string.IsNullOrEmpty(column))
                    continue;
                found.Add((table, column));
            }
        }

        return found
            .Distinct()
            .OrderBy(x => x.Table, StringComparer.Ordinal)
            .ThenBy(x => x.Column, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Rewrites every mapped DateTimeOffset TEXT column on this SQLite connection.
    /// Missing tables/columns are skipped. Nulls and already-canonical values stay put.
    /// Unparseable non-null values throw (fail closed).
    /// </summary>
    public static int Apply(DbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (connection is not SqliteConnection)
            return 0;

        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
            connection.Open();

        try
        {
            var changed = 0;
            foreach (var (table, column) in Columns)
            {
                if (!SqliteColumnExists(connection, table, column))
                    continue;
                changed += RewriteColumn(connection, table, column);
            }

            return changed;
        }
        finally
        {
            if (shouldClose)
                connection.Close();
        }
    }

    private static int RewriteColumn(DbConnection connection, string table, string column)
    {
        var changed = 0;
        using var select = connection.CreateCommand();
        select.CommandText = $"SELECT rowid, \"{column}\" FROM \"{table}\" WHERE \"{column}\" IS NOT NULL";
        using var reader = select.ExecuteReader();
        var updates = new List<(long RowId, string Next)>();
        while (reader.Read())
        {
            var rowId = reader.GetInt64(0);
            var current = reader.GetString(1);
            string? next;
            try
            {
                next = SqliteSortableUtcDateTimeOffsetConverter.NormalizeStoreValue(current);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    $"SQLite DateTimeOffset rewrite cannot parse {table}.{column} rowid={rowId}.",
                    ex);
            }

            if (next is null || string.Equals(current, next, StringComparison.Ordinal))
                continue;
            updates.Add((rowId, next));
        }

        reader.Dispose();

        foreach (var (rowId, next) in updates)
        {
            using var update = connection.CreateCommand();
            update.CommandText = $"UPDATE \"{table}\" SET \"{column}\" = $v WHERE rowid = $id";
            var v = update.CreateParameter();
            v.ParameterName = "$v";
            v.Value = next;
            update.Parameters.Add(v);
            var id = update.CreateParameter();
            id.ParameterName = "$id";
            id.Value = rowId;
            update.Parameters.Add(id);
            update.ExecuteNonQuery();
            changed++;
        }

        return changed;
    }

    private static bool SqliteColumnExists(DbConnection connection, string table, string column)
    {
        using var tables = connection.CreateCommand();
        tables.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $t LIMIT 1";
        var t = tables.CreateParameter();
        t.ParameterName = "$t";
        t.Value = table;
        tables.Parameters.Add(t);
        if (tables.ExecuteScalar() is null)
            return false;

        using var info = connection.CreateCommand();
        info.CommandText = $"PRAGMA table_info(\"{table}\")";
        using var reader = info.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
