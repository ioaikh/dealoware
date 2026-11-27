using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;

namespace Dealoware.Infrastructure.Persistence;

/// <summary>
/// The read-only check behind <c>dbcheck</c>: what a database holds up to a moment (the
/// cutoff), in a form that can be compared between two databases without printing any data.
/// Used by the restore test, which runs it against the live database (some minutes after the
/// cutoff) and against a copy restored to the cutoff.
/// <list type="bullet">
///   <item>For each table of the model, the number of rows created at or before the cutoff
///     (<see cref="CreatedColumns"/>; a table with no creation time counts all its rows), and
///     whether the engine hard-deletes rows from it (<see cref="HardDeleteTables"/>).</item>
///   <item>The newest migration's ID.</item>
///   <item>The known records: up to <see cref="KnownRecords"/> of the newest rows created at or
///     before the cutoff that have not changed since it (<see cref="ChangedColumns"/>), each as
///     a SHA-256 of its primary key and a SHA-256 of all its stored columns. A row changed on
///     the live database after the cutoff is left out there, so it cannot make two equal
///     backups look different; the restore test compares the records both sides hold.</item>
/// </list>
/// Everything runs in one read-only, repeatable-read transaction (<see cref="BeginReadOnlyAsync"/>),
/// which is rolled back. PostgreSQL only. No column value leaves this class: only counts,
/// hashes, table and column names and the migration ID.
/// </summary>
public static class DatabaseCheck
{
    public const string ApplicationName = "dealoware-dbcheck";

    /// <summary>The most known records given per table.</summary>
    public const int KnownRecords = 5;

    /// <summary>
    /// Each table of the model and the column that holds the time its row was created, or
    /// <c>null</c> when the table has none (its rows are then all counted). Every table of the
    /// model must be listed: <c>DatabaseCheckCommandTests</c> fails when one is missing.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string?> CreatedColumns =
        new SortedDictionary<string, string?>(StringComparer.Ordinal)
        {
            // Engine
            ["AcceptGrants"] = "CreatedAt",
            ["ApiKeyCredentials"] = "CreatedAt",
            ["Artifacts"] = "CreatedAt",
            ["ArtifactValues"] = null,
            ["EntityProperties"] = null,
            ["Negotiations"] = "CreatedAt",
            ["Offers"] = "CreatedAt",
            ["ParticipantBudgets"] = "CreatedAt",
            ["Participants"] = "CreatedAt",
            ["RevokedTokens"] = "RevokedAt",
            ["Strategies"] = "CreatedAt",
            ["SubjectEntities"] = null,
            ["TimePeriods"] = null,

            // Admin console
            ["AdminAuditLog"] = "Timestamp",
            ["AdminAuthFailureEvents"] = "OccurredAt",
            ["AdminAuthLockouts"] = null,
            ["AdminAuthTokens"] = "CreatedAt",
            ["AdminBootstrapTokens"] = "CreatedAt",
            ["AdminCoreOwnerAccounts"] = "CreatedAt",
            ["AdminCredentials"] = null,
            ["AdminDeleteConfirmTokens"] = "CreatedAt",
            ["AdminPasswordResetTokens"] = "CreatedAt",
            ["AdminPendingAuths"] = "CreatedAt",
            ["AdminRecoveryCodes"] = "CreatedAt",
            ["AdminResetIpCounters"] = null,
            ["AdminSessionRecoveryNotices"] = "CreatedAt",
            ["AdminSessions"] = "CreatedAt",
            ["AdminSessionStepUps"] = null,
        };

    /// <summary>
    /// The columns that are set to the current time when a row changes. A row is "unchanged
    /// since the cutoff" when each of them is null or at or before it. Tables not listed have
    /// no such column: their rows are written once (or the change leaves no time behind).
    /// <c>DatabaseCheckCommandTests</c> checks that each is a time column of the model.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> ChangedColumns =
        new SortedDictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["ApiKeyCredentials"] = ["RevokedAt"],
            ["Artifacts"] = ["UpdatedAt", "DeletedAt"],
            ["Negotiations"] = ["UpdatedAt", "DeletedAt"],
            ["Offers"] = ["UpdatedAt", "DeletedAt"],
            ["ParticipantBudgets"] = ["UpdatedAt"],
            ["Participants"] = ["UpdatedAt", "DeletedAt"],
            ["Strategies"] = ["UpdatedAt"],

            ["AdminAuthLockouts"] = ["StartedAt"],
            ["AdminAuthTokens"] = ["ConsumedAt"],
            ["AdminBootstrapTokens"] = ["ConsumedAt"],
            ["AdminCoreOwnerAccounts"] = ["FactorAttemptWindowStartedAt", "FactorLockedUntil", "TotpEnrolledAt"],
            ["AdminCredentials"] = ["PasswordUpdatedAt"],
            ["AdminDeleteConfirmTokens"] = ["ConsumedAt"],
            ["AdminPasswordResetTokens"] = ["ConsumedAt"],
            ["AdminPendingAuths"] = ["ConsumedAt"],
            ["AdminRecoveryCodes"] = ["UsedAt"],
            ["AdminResetIpCounters"] = ["WindowStartedAt", "LockedUntil"],
            ["AdminSessions"] = ["LastActivityAt"],
            ["AdminSessionStepUps"] = ["SteppedUpAt", "CodesIssuedAt"],
        };

    /// <summary>
    /// The tables the engine hard-deletes rows from (expired or used tokens, sessions, recovery
    /// codes, failure events). Their count on the live database may be lower than on a copy
    /// restored to an earlier moment. <c>DatabaseCheckCommandTests</c> scans the source for
    /// deletes and fails when one hits a table not listed here.
    /// </summary>
    public static readonly IReadOnlyList<string> HardDeleteTables =
    [
        "AdminAuthFailureEvents",
        "AdminDeleteConfirmTokens",
        "AdminRecoveryCodes",
        "AdminSessionRecoveryNotices",
        "AdminSessions",
        "RevokedTokens",
    ];

    private static readonly Regex Identifier = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Opens the connection if needed and starts the check's transaction: repeatable read (one
    /// snapshot for every query), read only (PostgreSQL refuses any write in it with SQLSTATE
    /// 25006), and fixed output settings so that the same row gives the same text on any
    /// session. The caller rolls it back (disposing it does).
    /// </summary>
    public static async Task<NpgsqlTransaction> BeginReadOnlyAsync(
        NpgsqlConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var tx = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            // SET TRANSACTION must come before any query of the transaction. SET LOCAL ends
            // with it. None of these writes anything.
            await using (var set = new NpgsqlCommand(
                             "SET TRANSACTION READ ONLY; " +
                             "SET LOCAL TIME ZONE 'UTC'; " +
                             "SET LOCAL DateStyle = 'ISO, YMD'; " +
                             "SET LOCAL IntervalStyle = 'postgres'; " +
                             "SET LOCAL extra_float_digits = 3; " +
                             "SET LOCAL bytea_output = 'hex'", connection, tx))
            {
                await set.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using var check = new NpgsqlCommand("SELECT current_setting('transaction_read_only')", connection, tx);
            if (!string.Equals(await check.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string, "on",
                    StringComparison.Ordinal))
            {
                throw new DatabaseStepException("the transaction is not read-only");
            }

            return tx;
        }
        catch
        {
            await tx.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Runs the check on <paramref name="connection"/> at <paramref name="cutoff"/>.</summary>
    public static async Task<DatabaseCheckResult> RunAsync(
        NpgsqlConnection connection, DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var at = cutoff.ToUniversalTime();

        await using var tx = await BeginReadOnlyAsync(connection, cancellationToken).ConfigureAwait(false);

        string? migration;
        await using (var cmd = new NpgsqlCommand(
                         $"SELECT \"MigrationId\" FROM {Quote(DatabaseMigrationBaseline.HistoryTableName)} " +
                         "ORDER BY \"MigrationId\" DESC LIMIT 1", connection, tx))
        {
            migration = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        }

        var tables = new List<DatabaseCheckTable>();
        foreach (var (table, created) in CreatedColumns)
        {
            var changed = ChangedColumns.TryGetValue(table, out var c) ? c : [];
            if (!Identifier.IsMatch(table) || (created is not null && !Identifier.IsMatch(created)) || !changed.All(Identifier.IsMatch))
                throw new DatabaseStepException("a table or column name in the check's lists is not a plain name");

            var keys = await PrimaryKeyAsync(connection, tx, table, cancellationToken).ConfigureAwait(false);
            var createdBy = created is null ? null : $"t.{Quote(created)} <= @cutoff";

            long rows;
            await using (var count = new NpgsqlCommand(
                             $"SELECT count(*) FROM public.{Quote(table)} t{(createdBy is null ? "" : " WHERE " + createdBy)}",
                             connection, tx))
            {
                count.Parameters.AddWithValue("cutoff", at);
                rows = (long)(await count.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
            }

            var known = new List<DatabaseCheckRecord>();
            if (rows > 0)
            {
                var filters = changed.Select(col => $"(t.{Quote(col)} IS NULL OR t.{Quote(col)} <= @cutoff)").ToList();
                if (createdBy is not null)
                    filters.Insert(0, createdBy);
                var where = filters.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", filters);
                var keyList = string.Join(", ", keys.Select(k => "t." + Quote(k)));
                var order = (created is null ? string.Empty : $"t.{Quote(created)} DESC, ") +
                            string.Join(", ", keys.Select(k => $"t.{Quote(k)} DESC"));
                await using var read = new NpgsqlCommand(
                    $"SELECT ROW({keyList})::text, t::text FROM public.{Quote(table)} t{where} ORDER BY {order} LIMIT {KnownRecords}",
                    connection, tx);
                read.Parameters.AddWithValue("cutoff", at);
                await using var reader = await read.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    known.Add(new DatabaseCheckRecord(Sha256(reader.GetString(0)), Sha256(reader.GetString(1))));
            }

            tables.Add(new DatabaseCheckTable(table, created, changed, HardDeleteTables.Contains(table), rows, known));
        }

        await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
        return new DatabaseCheckResult(at, migration, tables);
    }

    private static async Task<IReadOnlyList<string>> PrimaryKeyAsync(
        NpgsqlConnection connection, NpgsqlTransaction tx, string table, CancellationToken cancellationToken)
    {
        var columns = new List<string>();
        await using var cmd = new NpgsqlCommand(
            "SELECT a.attname FROM pg_index i " +
            "JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey) " +
            "WHERE i.indrelid = to_regclass(quote_ident('public') || '.' || quote_ident(@table)) AND i.indisprimary " +
            "ORDER BY array_position(i.indkey::int2[], a.attnum)", connection, tx);
        cmd.Parameters.AddWithValue("table", table);
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                columns.Add(reader.GetString(0));
        }

        if (columns.Count == 0)
            throw new DatabaseStepException($"table {table} is missing or has no primary key");
        return columns;
    }

    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static string Sha256(string text)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}

/// <summary>A known record: hashes only, never a value.</summary>
public sealed record DatabaseCheckRecord(string KeySha256, string RowSha256);

/// <summary>One table: its rows up to the cutoff, and its known records (newest first).</summary>
public sealed record DatabaseCheckTable(
    string Table,
    string? CreatedColumn,
    IReadOnlyList<string> ChangedColumns,
    bool HardDeletes,
    long Rows,
    IReadOnlyList<DatabaseCheckRecord> Known)
{
    public bool Equals(DatabaseCheckTable? other)
        => other is not null && Table == other.Table && CreatedColumn == other.CreatedColumn &&
           ChangedColumns.SequenceEqual(other.ChangedColumns) && HardDeletes == other.HardDeletes &&
           Rows == other.Rows && Known.SequenceEqual(other.Known);

    public override int GetHashCode() => HashCode.Combine(Table, Rows, Known.Count);
}

/// <summary>What <c>dbcheck</c> prints, as one JSON line (<see cref="ToJson"/>).</summary>
public sealed record DatabaseCheckResult(DateTimeOffset Cutoff, string? Migration, IReadOnlyList<DatabaseCheckTable> Tables)
{
    public string ToJson()
    {
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();
            json.WriteString("check", "dbcheck");
            json.WriteString("cutoff", Cutoff.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture));
            json.WriteString("migration", Migration);
            json.WriteStartArray("tables");
            foreach (var t in Tables)
            {
                json.WriteStartObject();
                json.WriteString("table", t.Table);
                json.WriteString("createdColumn", t.CreatedColumn);
                json.WriteStartArray("changedColumns");
                foreach (var c in t.ChangedColumns)
                    json.WriteStringValue(c);
                json.WriteEndArray();
                json.WriteBoolean("hardDeletes", t.HardDeletes);
                json.WriteNumber("rows", t.Rows);
                json.WriteStartArray("known");
                foreach (var k in t.Known)
                {
                    json.WriteStartObject();
                    json.WriteString("keySha256", k.KeySha256);
                    json.WriteString("rowSha256", k.RowSha256);
                    json.WriteEndObject();
                }

                json.WriteEndArray();
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
