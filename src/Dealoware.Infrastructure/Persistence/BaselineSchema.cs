namespace Dealoware.Infrastructure.Persistence;

/// <summary>Provider-neutral column kind for the frozen baseline model.</summary>
public enum BaselineColumnKind
{
    Guid,
    String,
    DateTimeOffset,
    Boolean,
    Int32,
    Int64,
    Decimal
}

public sealed record BaselineColumnSpec(
    string Table,
    string Name,
    BaselineColumnKind Kind,
    bool IsNullable);

public sealed record BaselinePrimaryKeySpec(string Table, IReadOnlyList<string> Columns);

public sealed record BaselineLiveColumn(
    string Table,
    string Name,
    string StoreType,
    bool IsNullable);

public sealed record BaselineLivePrimaryKey(string Table, IReadOnlyList<string> Columns);

public enum BaselineSchemaMismatchKind
{
    None,
    Column,
    PrimaryKey
}

public sealed record BaselineSchemaComparison(
    BaselineSchemaMismatchKind Kind,
    bool Matches);

/// <summary>
/// Frozen stamp schema (EnsureCreated / baseline plus
/// <c>20261006000100_AddAdminTablesAndSoftDelete</c>) column and primary-key
/// contract. Used only by the migrate one-shot stamp path.
/// </summary>
public static class BaselineSchema
{
    public static readonly IReadOnlyList<BaselineColumnSpec> Columns =
    [
        new("AcceptGrants", "Id", BaselineColumnKind.Guid, false),
        new("AcceptGrants", "OfferId", BaselineColumnKind.Guid, false),
        new("AcceptGrants", "NegotiationId", BaselineColumnKind.Guid, false),
        new("AcceptGrants", "GrantorSub", BaselineColumnKind.String, false),
        new("AcceptGrants", "GranteeSub", BaselineColumnKind.String, false),
        new("AcceptGrants", "CreatedAt", BaselineColumnKind.DateTimeOffset, false),

        new("AdminAuditLog", "Id", BaselineColumnKind.Guid, false),
        new("AdminAuditLog", "Timestamp", BaselineColumnKind.DateTimeOffset, false),
        new("AdminAuditLog", "Action", BaselineColumnKind.String, false),
        new("AdminAuditLog", "ActorEmail", BaselineColumnKind.String, false),
        new("AdminAuditLog", "IpHmac", BaselineColumnKind.String, false),
        new("AdminAuditLog", "EntityType", BaselineColumnKind.String, true),
        new("AdminAuditLog", "EntityId", BaselineColumnKind.Guid, true),
        new("AdminAuditLog", "ReasonClass", BaselineColumnKind.String, true),
        new("AdminAuditLog", "CorrelationId", BaselineColumnKind.Guid, true),
        new("AdminAuditLog", "BeforeSnapshot", BaselineColumnKind.String, true),
        new("AdminAuditLog", "AfterSnapshot", BaselineColumnKind.String, true),

        new("AdminSessions", "Id", BaselineColumnKind.Guid, false),
        new("AdminSessions", "Email", BaselineColumnKind.String, false),
        new("AdminSessions", "CreatedAt", BaselineColumnKind.DateTimeOffset, false),
        new("AdminSessions", "LastActivityAt", BaselineColumnKind.DateTimeOffset, false),
        new("AdminSessions", "AbsoluteExpiresAt", BaselineColumnKind.DateTimeOffset, false),
        new("AdminSessions", "TotpVerified", BaselineColumnKind.Boolean, false),
        new("AdminSessions", "IpHmac", BaselineColumnKind.String, false),

        new("ApiKeyCredentials", "Id", BaselineColumnKind.Guid, false),
        new("ApiKeyCredentials", "ParticipantId", BaselineColumnKind.Guid, false),
        new("ApiKeyCredentials", "KeyPrefix", BaselineColumnKind.String, false),
        new("ApiKeyCredentials", "KeyHash", BaselineColumnKind.String, false),
        new("ApiKeyCredentials", "CreatedAt", BaselineColumnKind.DateTimeOffset, false),
        new("ApiKeyCredentials", "RevokedAt", BaselineColumnKind.DateTimeOffset, true),

        new("Artifacts", "Id", BaselineColumnKind.Guid, false),
        new("Artifacts", "OwnerParticipantId", BaselineColumnKind.String, false),
        new("Artifacts", "Intent", BaselineColumnKind.String, false),
        new("Artifacts", "CreatedAt", BaselineColumnKind.DateTimeOffset, false),
        new("Artifacts", "Locations", BaselineColumnKind.String, false),
        new("Artifacts", "DeletedAt", BaselineColumnKind.DateTimeOffset, true),
        new("Artifacts", "Version", BaselineColumnKind.Int32, false),

        new("Negotiations", "Id", BaselineColumnKind.Guid, false),
        new("Negotiations", "ArtifactId", BaselineColumnKind.Guid, false),
        new("Negotiations", "PartyAParticipantId", BaselineColumnKind.String, false),
        new("Negotiations", "PartyBParticipantId", BaselineColumnKind.String, false),
        new("Negotiations", "PartyAIntent", BaselineColumnKind.String, false),
        new("Negotiations", "PartyBIntent", BaselineColumnKind.String, false),
        new("Negotiations", "Status", BaselineColumnKind.Int32, false),
        new("Negotiations", "StartsAt", BaselineColumnKind.DateTimeOffset, true),
        new("Negotiations", "EndsAt", BaselineColumnKind.DateTimeOffset, true),
        new("Negotiations", "CreatedAt", BaselineColumnKind.DateTimeOffset, false),
        new("Negotiations", "DeletedAt", BaselineColumnKind.DateTimeOffset, true),
        new("Negotiations", "Version", BaselineColumnKind.Int32, false),

        new("ParticipantBudgets", "Id", BaselineColumnKind.Guid, false),
        new("ParticipantBudgets", "ParticipantSub", BaselineColumnKind.String, false),
        new("ParticipantBudgets", "LimitUnits", BaselineColumnKind.Int64, false),
        new("ParticipantBudgets", "UsedUnits", BaselineColumnKind.Int64, false),
        new("ParticipantBudgets", "CreatedAt", BaselineColumnKind.DateTimeOffset, false),
        new("ParticipantBudgets", "UpdatedAt", BaselineColumnKind.DateTimeOffset, false),

        new("Participants", "Id", BaselineColumnKind.Guid, false),
        new("Participants", "Sub", BaselineColumnKind.String, false),
        new("Participants", "DisplayName", BaselineColumnKind.String, true),
        new("Participants", "LoginEmail", BaselineColumnKind.String, true),
        new("Participants", "ContactEmail", BaselineColumnKind.String, true),
        new("Participants", "CreatedAt", BaselineColumnKind.DateTimeOffset, false),
        new("Participants", "IsActive", BaselineColumnKind.Boolean, false),
        new("Participants", "DeletedAt", BaselineColumnKind.DateTimeOffset, true),
        new("Participants", "Version", BaselineColumnKind.Int32, false),

        new("RevokedTokens", "Jti", BaselineColumnKind.String, false),
        new("RevokedTokens", "Sub", BaselineColumnKind.String, false),
        new("RevokedTokens", "RevokedAt", BaselineColumnKind.DateTimeOffset, false),
        new("RevokedTokens", "ExpiresAt", BaselineColumnKind.DateTimeOffset, false),

        new("Strategies", "Id", BaselineColumnKind.Guid, false),
        new("Strategies", "OwnerParticipantId", BaselineColumnKind.String, false),
        new("Strategies", "Name", BaselineColumnKind.String, true),
        new("Strategies", "StrategyBody", BaselineColumnKind.String, true),
        new("Strategies", "CreatedAt", BaselineColumnKind.DateTimeOffset, false),
        new("Strategies", "UpdatedAt", BaselineColumnKind.DateTimeOffset, false),
        new("Strategies", "IsActive", BaselineColumnKind.Boolean, false),

        new("ArtifactValues", "Id", BaselineColumnKind.Guid, false),
        new("ArtifactValues", "Amount", BaselineColumnKind.Decimal, false),
        new("ArtifactValues", "Currency", BaselineColumnKind.String, false),
        new("ArtifactValues", "ArtifactId", BaselineColumnKind.Guid, true),

        new("SubjectEntities", "Id", BaselineColumnKind.Guid, false),
        new("SubjectEntities", "Name", BaselineColumnKind.String, false),
        new("SubjectEntities", "Description", BaselineColumnKind.String, false),
        new("SubjectEntities", "ArtifactId", BaselineColumnKind.Guid, true),
        new("SubjectEntities", "Facts", BaselineColumnKind.String, false),

        new("TimePeriods", "Id", BaselineColumnKind.Guid, false),
        new("TimePeriods", "Start", BaselineColumnKind.DateTimeOffset, false),
        new("TimePeriods", "End", BaselineColumnKind.DateTimeOffset, false),
        new("TimePeriods", "ArtifactId", BaselineColumnKind.Guid, true),

        new("Offers", "Id", BaselineColumnKind.Guid, false),
        new("Offers", "NegotiationId", BaselineColumnKind.Guid, false),
        new("Offers", "FromParticipantId", BaselineColumnKind.String, false),
        new("Offers", "ToParticipantId", BaselineColumnKind.String, false),
        new("Offers", "Status", BaselineColumnKind.Int32, false),
        new("Offers", "Amount", BaselineColumnKind.Decimal, true),
        new("Offers", "Currency", BaselineColumnKind.String, true),
        new("Offers", "Terms", BaselineColumnKind.String, true),
        new("Offers", "CreatedAt", BaselineColumnKind.DateTimeOffset, false),
        new("Offers", "DeletedAt", BaselineColumnKind.DateTimeOffset, true),
        new("Offers", "Version", BaselineColumnKind.Int32, false),

        new("EntityProperties", "Id", BaselineColumnKind.Guid, false),
        new("EntityProperties", "Name", BaselineColumnKind.String, false),
        new("EntityProperties", "Type", BaselineColumnKind.String, false),
        new("EntityProperties", "Value", BaselineColumnKind.String, false),
        new("EntityProperties", "SubjectEntityId", BaselineColumnKind.Guid, true)
    ];

    public static readonly IReadOnlyList<BaselinePrimaryKeySpec> PrimaryKeys =
    [
        new("AcceptGrants", ["Id"]),
        new("AdminAuditLog", ["Id"]),
        new("AdminSessions", ["Id"]),
        new("ApiKeyCredentials", ["Id"]),
        new("Artifacts", ["Id"]),
        new("Negotiations", ["Id"]),
        new("ParticipantBudgets", ["Id"]),
        new("Participants", ["Id"]),
        new("RevokedTokens", ["Jti"]),
        new("Strategies", ["Id"]),
        new("ArtifactValues", ["Id"]),
        new("SubjectEntities", ["Id"]),
        new("TimePeriods", ["Id"]),
        new("Offers", ["Id"]),
        new("EntityProperties", ["Id"])
    ];

    public static BaselineSchemaComparison Compare(
        IReadOnlyCollection<BaselineLiveColumn> liveColumns,
        IReadOnlyCollection<BaselineLivePrimaryKey> liveKeys,
        bool postgres = false)
    {
        ArgumentNullException.ThrowIfNull(liveColumns);
        ArgumentNullException.ThrowIfNull(liveKeys);

        var names = postgres ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

        var liveByTable = liveColumns
            .GroupBy(c => c.Table, names)
            .ToDictionary(g => g.Key, g => g.ToList(), names);

        foreach (var tableGroup in Columns.GroupBy(c => c.Table, names))
        {
            if (!liveByTable.TryGetValue(tableGroup.Key, out var actual))
            {
                return new BaselineSchemaComparison(BaselineSchemaMismatchKind.Column, Matches: false);
            }

            var actualByName = actual.ToDictionary(c => c.Name, names);
            if (actualByName.Count != tableGroup.Count())
            {
                return new BaselineSchemaComparison(BaselineSchemaMismatchKind.Column, Matches: false);
            }

            foreach (var expected in tableGroup)
            {
                if (!actualByName.TryGetValue(expected.Name, out var live)
                    || live.IsNullable != expected.IsNullable
                    || !StoreTypeMatches(expected.Kind, live.StoreType, postgres))
                {
                    return new BaselineSchemaComparison(BaselineSchemaMismatchKind.Column, Matches: false);
                }
            }
        }

        var liveKeyByTable = liveKeys.ToDictionary(k => k.Table, names);
        foreach (var expected in PrimaryKeys)
        {
            if (!liveKeyByTable.TryGetValue(expected.Table, out var live)
                || !expected.Columns.SequenceEqual(live.Columns, names))
            {
                return new BaselineSchemaComparison(BaselineSchemaMismatchKind.PrimaryKey, Matches: false);
            }
        }

        return new BaselineSchemaComparison(BaselineSchemaMismatchKind.None, Matches: true);
    }

    internal static bool StoreTypeMatches(BaselineColumnKind kind, string storeType, bool postgres = false)
        => postgres
            ? PostgresStoreTypeMatches(kind, storeType)
            : SqliteStoreTypeMatches(kind, storeType);

    private static bool PostgresStoreTypeMatches(BaselineColumnKind kind, string storeType)
    {
        var t = storeType.Trim().ToLowerInvariant();
        return kind switch
        {
            BaselineColumnKind.Guid => t is "uuid",
            BaselineColumnKind.String => t is "text" or "character varying",
            BaselineColumnKind.DateTimeOffset => t is "timestamp with time zone",
            BaselineColumnKind.Boolean => t is "boolean",
            BaselineColumnKind.Int32 => t is "integer",
            BaselineColumnKind.Int64 => t is "bigint",
            BaselineColumnKind.Decimal => t is "numeric",
            _ => false
        };
    }

    private static bool SqliteStoreTypeMatches(BaselineColumnKind kind, string storeType)
    {
        var t = storeType.Trim().ToUpperInvariant();
        return kind switch
        {
            BaselineColumnKind.Guid =>
                t is "TEXT" or "UUID" or "GUID" or "UNIQUEIDENTIFIER" or "BLOB",
            BaselineColumnKind.String =>
                t.Contains("CHAR", StringComparison.Ordinal)
                || t is "TEXT" or "CLOB" or "NVARCHAR" or "VARCHAR",
            BaselineColumnKind.DateTimeOffset =>
                t is "TEXT" or "DATETIME" or "DATETIMEOFFSET"
                || t.Contains("TIMESTAMP", StringComparison.Ordinal),
            BaselineColumnKind.Boolean =>
                t is "INTEGER" or "BOOLEAN" or "BOOL" or "BIT",
            BaselineColumnKind.Int32 =>
                t is "INTEGER" or "INT" or "INT4",
            BaselineColumnKind.Int64 =>
                t is "INTEGER" or "BIGINT" or "INT8" or "LONG",
            BaselineColumnKind.Decimal =>
                t.Contains("DECIMAL", StringComparison.Ordinal)
                || t.Contains("NUMERIC", StringComparison.Ordinal)
                || t is "TEXT" or "REAL",
            _ => false
        };
    }
}
