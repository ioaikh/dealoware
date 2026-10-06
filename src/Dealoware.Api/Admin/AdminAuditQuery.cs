using Dealoware.Domain.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// Parses GET /admin/api/audit query parameters. Unknown sort keys, non-numeric
/// paging, and over-long string filters return a generic 400.
/// </summary>
public static class AdminAuditQuery
{
    private static readonly HashSet<string> SortAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "timestamp",
        "-timestamp",
        "action",
        "-action",
        "actorEmail",
        "-actorEmail",
        "entityType",
        "-entityType"
    };

    public static bool TryParse(
        IQueryCollection query,
        out AdminAuditListCriteria criteria,
        out IResult? error)
    {
        criteria = new AdminAuditListCriteria();
        error = null;

        if (!TryParseNonNegativeInt(query, "offset", optionalDefault: 0, out var offset, out error))
            return false;
        if (!TryParseLimit(query, out var limit, out error))
            return false;

        var sort = query["sort"].ToString();
        if (string.IsNullOrWhiteSpace(sort))
            sort = "-timestamp";
        if (sort.Length > AdminAuditListCriteria.MaxFilterLength || !SortAllowlist.Contains(sort))
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        if (!TryOptionalCapped(query, "action", AdminAuditListCriteria.MaxActionLength, out var action, out error)
            || !TryOptionalCapped(query, "actorEmail", AdminAuditListCriteria.MaxFilterLength, out var actorEmail, out error)
            || !TryOptionalCapped(query, "entityType", AdminAuditListCriteria.MaxActionLength, out var entityType, out error)
            || !TryOptionalCapped(query, "reasonClass", AdminAuditListCriteria.MaxActionLength, out var reasonClass, out error)
            || !TryOptionalCapped(query, "q", AdminAuditListCriteria.MaxQLength, out var q, out error))
        {
            return false;
        }

        if (!TryOptionalGuid(query, "entityId", out var entityId, out error)
            || !TryOptionalGuid(query, "correlationId", out var correlationId, out error)
            || !TryOptionalTimestamp(query, "from", out var from, out error)
            || !TryOptionalTimestamp(query, "to", out var to, out error))
        {
            return false;
        }

        criteria = new AdminAuditListCriteria
        {
            Offset = offset,
            Limit = limit,
            Sort = sort,
            Action = action,
            ActorEmail = actorEmail,
            EntityType = entityType,
            EntityId = entityId,
            ReasonClass = reasonClass,
            CorrelationId = correlationId,
            From = from,
            To = to,
            Q = q
        };
        return true;
    }

    private static bool TryParseLimit(IQueryCollection query, out int limit, out IResult? error)
    {
        limit = AdminAuditListCriteria.DefaultLimit;
        error = null;
        var raw = query["limit"].ToString();
        if (string.IsNullOrWhiteSpace(raw))
            return true;
        if (!int.TryParse(raw, out var parsed) || parsed < 0)
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        limit = Math.Min(parsed, AdminAuditListCriteria.MaxLimit);
        return true;
    }

    private static bool TryParseNonNegativeInt(
        IQueryCollection query,
        string name,
        int optionalDefault,
        out int value,
        out IResult? error)
    {
        value = optionalDefault;
        error = null;
        var raw = query[name].ToString();
        if (string.IsNullOrWhiteSpace(raw))
            return true;
        if (!int.TryParse(raw, out var parsed) || parsed < 0)
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool TryOptionalCapped(
        IQueryCollection query,
        string name,
        int maxLength,
        out string? value,
        out IResult? error)
    {
        value = null;
        error = null;
        var raw = query[name].ToString();
        if (string.IsNullOrEmpty(raw))
            return true;
        if (raw.Length > maxLength)
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        value = raw;
        return true;
    }

    private static bool TryOptionalGuid(
        IQueryCollection query,
        string name,
        out Guid? value,
        out IResult? error)
    {
        value = null;
        error = null;
        var raw = query[name].ToString();
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

    private static bool TryOptionalTimestamp(
        IQueryCollection query,
        string name,
        out DateTimeOffset? value,
        out IResult? error)
    {
        value = null;
        error = null;
        var raw = query[name].ToString();
        if (string.IsNullOrWhiteSpace(raw))
            return true;
        if (raw.Length > AdminAuditListCriteria.MaxFilterLength
            || !DateTimeOffset.TryParse(raw, out var parsed))
        {
            error = AdminDeny.BadRequestResult();
            return false;
        }

        value = parsed.ToUniversalTime();
        return true;
    }
}
