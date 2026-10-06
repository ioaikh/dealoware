using System.Globalization;

namespace Dealoware.Api.Admin;

/// <summary>
/// Parsed list query. Sort keys come from a fixed allowlist mapped to columns.
/// </summary>
public sealed class AdminListQuery
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;
    public const int MaxQueryLength = 100;
    public const int MaxFilterLength = 200;

    public string? Q { get; init; }
    public string Sort { get; init; } = "created";
    public bool Descending { get; init; } = true;
    public int Offset { get; init; }
    public int Limit { get; init; } = DefaultLimit;
    public bool IncludeDeleted { get; init; }
    public string? Status { get; init; }
    public string? Participant { get; init; }
    public Guid? ArtifactId { get; init; }
    public Guid? NegotiationId { get; init; }
    public decimal? AmountMin { get; init; }
    public decimal? AmountMax { get; init; }
    public DateTimeOffset? CreatedFrom { get; init; }
    public DateTimeOffset? CreatedTo { get; init; }
    public DateTimeOffset? UpdatedFrom { get; init; }
    public DateTimeOffset? UpdatedTo { get; init; }

    public static readonly HashSet<string> ParticipantSortKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "created", "createdAt", "updated", "updatedAt", "displayName", "name"
    };

    public static readonly HashSet<string> ArtifactSortKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "created", "createdAt", "updated", "updatedAt", "name", "artifact"
    };

    public static readonly HashSet<string> NegotiationSortKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "created", "createdAt", "updated", "updatedAt", "status", "participant",
        "artifact", "id", "negotiationId"
    };

    public static readonly HashSet<string> OfferSortKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "created", "createdAt", "updated", "updatedAt", "status", "participant",
        "artifact", "amount", "value", "price", "negotiationId", "id"
    };

    public static readonly HashSet<string> NegotiationAmountSortKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "amount", "value", "price"
    };

    public static bool TryParse(
        IQueryCollection query,
        HashSet<string> allowedSortKeys,
        bool allowAmount,
        out AdminListQuery parsed,
        out IResult? error)
    {
        parsed = null!;
        error = null;

        if (!TryParseNonNegativeInt(query, "offset", optionalDefault: 0, out var offset, out error))
            return false;

        if (!TryParseLimit(query, out var limit, out error))
            return false;

        if (!TryParseOptionalBool(query, "includeDeleted", out var includeDeleted, out error))
            return false;

        var sortRaw = query["sort"].FirstOrDefault();
        var sort = string.IsNullOrWhiteSpace(sortRaw) ? "created" : sortRaw.Trim();
        if (!allowedSortKeys.Contains(sort))
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        if (!allowAmount && NegotiationAmountSortKeys.Contains(sort))
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        var dirRaw = query["dir"].FirstOrDefault() ?? query["order"].FirstOrDefault();
        var descending = true;
        if (!string.IsNullOrWhiteSpace(dirRaw))
        {
            if (dirRaw.Equals("asc", StringComparison.OrdinalIgnoreCase))
                descending = false;
            else if (dirRaw.Equals("desc", StringComparison.OrdinalIgnoreCase))
                descending = true;
            else
            {
                error = AdminDeny.BadRequestResult();
                return false;
            }
        }

        if (!TryParseOptionalGuid(query, "artifact", out var artifactId, out error))
            return false;
        if (!TryParseOptionalGuid(query, "artifactId", out var artifactIdAlias, out error))
            return false;
        artifactId ??= artifactIdAlias;

        if (!TryParseOptionalGuid(query, "negotiationId", out var negotiationId, out error))
            return false;
        if (!TryParseOptionalGuid(query, "negotiation", out var negotiationAlias, out error))
            return false;
        negotiationId ??= negotiationAlias;

        decimal? amountMin = null;
        decimal? amountMax = null;
        if (query.ContainsKey("amountMin") || query.ContainsKey("amountMax")
            || query.ContainsKey("amount") || query.ContainsKey("value") || query.ContainsKey("price"))
        {
            if (!allowAmount)
            {
                error = AdminDeny.BadRequestResult();
                return false;
            }

            if (!TryParseOptionalDecimal(query, "amountMin", out amountMin, out error))
                return false;
            if (!TryParseOptionalDecimal(query, "amountMax", out amountMax, out error))
                return false;
            if (!TryParseOptionalDecimal(query, "amount", out var amountEq, out error))
                return false;
            if (amountEq.HasValue)
            {
                amountMin = amountEq;
                amountMax = amountEq;
            }
        }

        if (!TryParseOptionalDate(query, "createdFrom", out var createdFrom, out error))
            return false;
        if (!TryParseOptionalDate(query, "createdTo", out var createdTo, out error))
            return false;
        if (!TryParseOptionalDate(query, "updatedFrom", out var updatedFrom, out error))
            return false;
        if (!TryParseOptionalDate(query, "updatedTo", out var updatedTo, out error))
            return false;

        var status = query["status"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(status))
            status = null;
        else
            status = status.Trim();
        if (status is { Length: > MaxFilterLength })
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        var participant = query["participant"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(participant))
            participant = null;
        else
            participant = participant.Trim();
        if (participant is { Length: > MaxFilterLength })
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        var q = query["q"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(q))
            q = null;
        else
            q = q.Trim();
        if (q is { Length: > MaxQueryLength })
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        parsed = new AdminListQuery
        {
            Q = q,
            Sort = sort,
            Descending = descending,
            Offset = offset,
            Limit = limit,
            IncludeDeleted = includeDeleted,
            Status = status,
            Participant = participant,
            ArtifactId = artifactId,
            NegotiationId = negotiationId,
            AmountMin = amountMin,
            AmountMax = amountMax,
            CreatedFrom = createdFrom,
            CreatedTo = createdTo,
            UpdatedFrom = updatedFrom,
            UpdatedTo = updatedTo
        };
        return true;
    }

    private static bool TryParseLimit(IQueryCollection query, out int limit, out IResult? error)
    {
        limit = DefaultLimit;
        error = null;
        if (!query.ContainsKey("limit"))
            return true;

        var raw = query["limit"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        limit = Math.Min(parsed, MaxLimit);
        return true;
    }

    private static bool TryParseNonNegativeInt(
        IQueryCollection query,
        string key,
        int optionalDefault,
        out int value,
        out IResult? error)
    {
        value = optionalDefault;
        error = null;
        if (!query.ContainsKey(key))
            return true;

        var raw = query[key].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool TryParseOptionalBool(
        IQueryCollection query,
        string key,
        out bool value,
        out IResult? error)
    {
        value = false;
        error = null;
        if (!query.ContainsKey(key))
            return true;

        var raw = query[key].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (raw.Equals("true", StringComparison.OrdinalIgnoreCase) || raw == "1")
        {
            value = true;
            return true;
        }

        if (raw.Equals("false", StringComparison.OrdinalIgnoreCase) || raw == "0")
        {
            value = false;
            return true;
        }

        error = AdminDeny.BadRequestResult();
        return false;
    }

    private static bool TryParseOptionalGuid(
        IQueryCollection query,
        string key,
        out Guid? value,
        out IResult? error)
    {
        value = null;
        error = null;
        if (!query.ContainsKey(key))
            return true;

        var raw = query[key].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (!Guid.TryParse(raw, out var parsed))
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool TryParseOptionalDecimal(
        IQueryCollection query,
        string key,
        out decimal? value,
        out IResult? error)
    {
        value = null;
        error = null;
        if (!query.ContainsKey(key))
            return true;

        var raw = query[key].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool TryParseOptionalDate(
        IQueryCollection query,
        string key,
        out DateTimeOffset? value,
        out IResult? error)
    {
        value = null;
        error = null;
        if (!query.ContainsKey(key))
            return true;

        var raw = query[key].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        value = parsed.ToUniversalTime();
        return true;
    }

    public static string NormalizeSort(string sort)
    {
        if (sort.Equals("createdAt", StringComparison.OrdinalIgnoreCase))
            return "created";
        if (sort.Equals("updatedAt", StringComparison.OrdinalIgnoreCase))
            return "updated";
        if (sort.Equals("value", StringComparison.OrdinalIgnoreCase)
            || sort.Equals("price", StringComparison.OrdinalIgnoreCase))
            return "amount";
        if (sort.Equals("name", StringComparison.OrdinalIgnoreCase)
            || sort.Equals("displayName", StringComparison.OrdinalIgnoreCase))
            return "name";
        return sort.ToLowerInvariant();
    }
}
