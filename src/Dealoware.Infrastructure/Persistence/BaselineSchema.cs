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
/// Frozen baseline (EnsureCreated / <c>20261005000000_Baseline</c>) column and
/// primary-key contract. Used only by the migrate one-shot stamp path.
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

        new("EntityProperties", "Id", BaselineColumnKind.Guid, false),
        new("EntityProperties", "Name", BaselineColumnKind.String, false),
        new("EntityProperties", "Type", BaselineColumnKind.String, false),
        new("EntityProperties", "Value", BaselineColumnKind.String, false),
        new("EntityProperties", "SubjectEntityId", BaselineColumnKind.Guid, true)
    ];

    public static readonly IReadOnlyList<BaselinePrimaryKeySpec> PrimaryKeys =
    [
        new("AcceptGrants", ["Id"]),
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
        IReadOnlyCollection<BaselineLivePrimaryKey> liveKeys)
    {
        ArgumentNullException.ThrowIfNull(liveColumns);
        ArgumentNullException.ThrowIfNull(liveKeys);

        var liveByTable = liveColumns
            .GroupBy(c => c.Table, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var tableGroup in Columns.GroupBy(c => c.Table, StringComparer.OrdinalIgnoreCase))
        {
            if (!liveByTable.TryGetValue(tableGroup.Key, out var actual))
            {
                return new BaselineSchemaComparison(BaselineSchemaMismatchKind.Column, Matches: false);
            }

            var actualByName = actual.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
            if (actualByName.Count != tableGroup.Count())
            {
                return new BaselineSchemaComparison(BaselineSchemaMismatchKind.Column, Matches: false);
            }

            foreach (var expected in tableGroup)
            {
                if (!actualByName.TryGetValue(expected.Name, out var live)
                    || live.IsNullable != expected.IsNullable
                    || !StoreTypeMatches(expected.Kind, live.StoreType))
                {
                    return new BaselineSchemaComparison(BaselineSchemaMismatchKind.Column, Matches: false);
                }
            }
        }

        var liveKeyByTable = liveKeys.ToDictionary(k => k.Table, StringComparer.OrdinalIgnoreCase);
        foreach (var expected in PrimaryKeys)
        {
            if (!liveKeyByTable.TryGetValue(expected.Table, out var live)
                || !expected.Columns.SequenceEqual(live.Columns, StringComparer.OrdinalIgnoreCase))
            {
                return new BaselineSchemaComparison(BaselineSchemaMismatchKind.PrimaryKey, Matches: false);
            }
        }

        return new BaselineSchemaComparison(BaselineSchemaMismatchKind.None, Matches: true);
    }

    internal static bool StoreTypeMatches(BaselineColumnKind kind, string storeType)
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
