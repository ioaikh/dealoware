using System.Text.Json;
using Dealoware.Api.Admin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Dealoware.Api.Tests;

/// <summary>
/// Test-only GET /admin/api/audit stub bound to Step 8 PR #32 @ db22afba.
/// Drop this stub after #32 merges: rebase onto main and use the real endpoint.
/// GET /admin/api/audit/{id} is a viewer-only adapter for S-D2; #32 is list-only
/// and already includes snapshots on each list item.
/// </summary>
public static class AdminAuditApiStub
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;
    public const int IpHmacPrefixLength = 12;
    public const int MaxQLength = 100;
    public const int MaxFilterLength = 200;
    public const int MaxActionLength = 64;

    public static readonly Guid EditId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DeleteId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid AuthFailId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid DeletedEntityAuditId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid ExistingParticipantId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid DeletedParticipantId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid MissingId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public const string Actor = "io@aiknowhow.com";

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

    public static IReadOnlyList<StubEntry> Seed { get; } = CreateSeed();

    public static async Task WriteAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (IsMutation(context.Request.Method)
            && path.Equals("/admin/api/audit", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        if (HttpMethods.IsGet(context.Request.Method)
            && path.Equals("/admin/api/audit", StringComparison.OrdinalIgnoreCase))
        {
            await WriteListAsync(context);
            return;
        }

        if (HttpMethods.IsGet(context.Request.Method)
            && path.StartsWith("/admin/api/audit/", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(path["/admin/api/audit/".Length..], out var id))
        {
            await WriteDetailAsync(context, id);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    private static bool IsMutation(string method) =>
        HttpMethods.IsPost(method)
        || HttpMethods.IsPut(method)
        || HttpMethods.IsPatch(method)
        || HttpMethods.IsDelete(method);

    private static async Task WriteListAsync(HttpContext context)
    {
        if (!TryParseQuery(context.Request.Query, out var query, out var badRequest))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("""{"error":"Bad request"}""");
            _ = badRequest;
            return;
        }

        var filtered = ApplyFilters(Seed, query);
        var sorted = ApplySort(filtered, query.Sort);
        var page = sorted.Skip(query.Offset).Take(query.Limit).Select(ToReadModel).ToList();
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            items = page,
            total = filtered.Count,
            offset = query.Offset,
            limit = query.Limit
        }));
    }

    private static async Task WriteDetailAsync(HttpContext context, Guid id)
    {
        var entry = Seed.FirstOrDefault(item => item.Id == id);
        if (entry is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("""{"error":"Not found"}""");
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(ToReadModel(entry)));
    }

    private static bool TryParseQuery(IQueryCollection query, out ParsedQuery parsed, out bool badRequest)
    {
        parsed = new ParsedQuery(0, DefaultLimit, "-timestamp", null, null, null, null, null, null, null, null, null);
        badRequest = false;

        if (!TryParseNonNegativeInt(query, "offset", 0, out var offset))
        {
            badRequest = true;
            return false;
        }

        if (!TryParseLimit(query, out var limit))
        {
            badRequest = true;
            return false;
        }

        var sort = query["sort"].ToString();
        if (string.IsNullOrWhiteSpace(sort))
            sort = "-timestamp";
        if (sort.Length > MaxFilterLength || !SortAllowlist.Contains(sort))
        {
            badRequest = true;
            return false;
        }

        if (!TryOptionalCapped(query, "action", MaxActionLength, out var action)
            || !TryOptionalCapped(query, "actorEmail", MaxFilterLength, out var actorEmail)
            || !TryOptionalCapped(query, "entityType", MaxActionLength, out var entityType)
            || !TryOptionalCapped(query, "reasonClass", MaxActionLength, out var reasonClass)
            || !TryOptionalCapped(query, "q", MaxQLength, out var q)
            || !TryOptionalGuid(query, "entityId", out var entityId)
            || !TryOptionalGuid(query, "correlationId", out var correlationId)
            || !TryOptionalTimestamp(query, "from", out var from)
            || !TryOptionalTimestamp(query, "to", out var to))
        {
            badRequest = true;
            return false;
        }

        parsed = new ParsedQuery(
            offset, limit, sort, action, actorEmail, entityType, entityId,
            reasonClass, correlationId, from, to, q);
        return true;
    }

    private static bool TryParseLimit(IQueryCollection query, out int limit)
    {
        limit = DefaultLimit;
        var raw = query["limit"].ToString();
        if (string.IsNullOrWhiteSpace(raw))
            return true;
        if (!int.TryParse(raw, out var parsed) || parsed < 0)
            return false;
        limit = Math.Min(parsed, MaxLimit);
        return true;
    }

    private static bool TryParseNonNegativeInt(IQueryCollection query, string name, int optionalDefault, out int value)
    {
        value = optionalDefault;
        var raw = query[name].ToString();
        if (string.IsNullOrWhiteSpace(raw))
            return true;
        if (!int.TryParse(raw, out var parsed) || parsed < 0)
            return false;
        value = parsed;
        return true;
    }

    private static bool TryOptionalCapped(IQueryCollection query, string name, int maxLength, out string? value)
    {
        value = null;
        var raw = query[name].ToString();
        if (string.IsNullOrEmpty(raw))
            return true;
        if (raw.Length > maxLength)
            return false;
        value = raw;
        return true;
    }

    private static bool TryOptionalGuid(IQueryCollection query, string name, out Guid? value)
    {
        value = null;
        var raw = query[name].ToString();
        if (string.IsNullOrWhiteSpace(raw))
            return true;
        if (!Guid.TryParse(raw, out var parsed))
            return false;
        value = parsed;
        return true;
    }

    private static bool TryOptionalTimestamp(IQueryCollection query, string name, out DateTimeOffset? value)
    {
        value = null;
        var raw = query[name].ToString();
        if (string.IsNullOrWhiteSpace(raw))
            return true;
        if (raw.Length > MaxFilterLength || !DateTimeOffset.TryParse(raw, out var parsed))
            return false;
        value = parsed.ToUniversalTime();
        return true;
    }

    private static List<StubEntry> ApplyFilters(IEnumerable<StubEntry> source, ParsedQuery query)
    {
        return source.Where(entry =>
        {
            if (!string.IsNullOrEmpty(query.Action) && entry.Action != query.Action)
                return false;
            if (!string.IsNullOrEmpty(query.ActorEmail)
                && !entry.Actor.Contains(query.ActorEmail, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrEmpty(query.EntityType) && entry.EntityType != query.EntityType)
                return false;
            if (query.EntityId is { } entityId && entry.EntityId != entityId)
                return false;
            if (!string.IsNullOrEmpty(query.ReasonClass) && entry.ReasonClass != query.ReasonClass)
                return false;
            if (query.CorrelationId is { } correlationId && entry.CorrelationId != correlationId)
                return false;
            if (query.From is { } from && entry.Timestamp < from)
                return false;
            if (query.To is { } to && entry.Timestamp > to)
                return false;
            if (!string.IsNullOrEmpty(query.Q))
            {
                var q = query.Q;
                var hit = entry.Action.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || entry.Actor.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (entry.EntityType is not null
                        && entry.EntityType.Contains(q, StringComparison.OrdinalIgnoreCase));
                if (!hit)
                    return false;
            }

            return true;
        }).ToList();
    }

    private static IEnumerable<StubEntry> ApplySort(IEnumerable<StubEntry> source, string sort)
    {
        return sort.ToLowerInvariant() switch
        {
            "timestamp" => source.OrderBy(e => e.Timestamp).ThenBy(e => e.Id),
            "action" => source.OrderBy(e => e.Action).ThenBy(e => e.Id),
            "-action" => source.OrderByDescending(e => e.Action).ThenBy(e => e.Id),
            "actoremail" => source.OrderBy(e => e.Actor).ThenBy(e => e.Id),
            "-actoremail" => source.OrderByDescending(e => e.Actor).ThenBy(e => e.Id),
            "entitytype" => source.OrderBy(e => e.EntityType).ThenBy(e => e.Id),
            "-entitytype" => source.OrderByDescending(e => e.EntityType).ThenBy(e => e.Id),
            _ => source.OrderByDescending(e => e.Timestamp).ThenBy(e => e.Id)
        };
    }

    private static object ToReadModel(StubEntry entry) => new
    {
        id = entry.Id,
        timestamp = entry.Timestamp,
        action = entry.Action,
        actorEmail = entry.Actor,
        ipHmacPrefix = Prefix(entry.IpHmacPrefix),
        entityType = entry.EntityType,
        entityId = entry.EntityId,
        reasonClass = entry.ReasonClass,
        correlationId = entry.CorrelationId,
        beforeSnapshot = SerializeSnapshot(entry.Before),
        afterSnapshot = SerializeSnapshot(entry.After)
    };

    private static string Prefix(string ipHmac)
    {
        if (string.IsNullOrEmpty(ipHmac))
            return string.Empty;
        return ipHmac.Length <= IpHmacPrefixLength
            ? ipHmac
            : ipHmac[..IpHmacPrefixLength];
    }

    private static readonly HashSet<string> SecretKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwordHash", "totp", "totpSecret", "recovery", "recoveryCode",
        "recoveryCodes", "hmac", "hmacKey", "secret", "apiKey", "turnstile",
        "turnstileToken", "ip", "StrategyBody"
    };

    private static string? SerializeSnapshot(Dictionary<string, string?>? snapshot)
    {
        if (snapshot is null)
            return null;
        var allowed = snapshot
            .Where(pair => !SecretKeys.Contains(pair.Key) && !LooksLikeIp(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        return JsonSerializer.Serialize(allowed);
    }

    private static bool LooksLikeIp(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && (System.Net.IPAddress.TryParse(value, out _)
            || System.Text.RegularExpressions.Regex.IsMatch(value, @"^(?:\d{1,3}\.){3}\d{1,3}$"));

    private static List<StubEntry> CreateSeed()
    {
        var rows = new List<StubEntry>
        {
            new(
                EditId,
                new DateTimeOffset(2026, 10, 5, 23, 55, 0, TimeSpan.Zero),
                Actor,
                "entity_edit",
                "Participant",
                ExistingParticipantId,
                ReasonClass: null,
                EntityExists: true,
                IpHmacPrefix: "a1b2c3d4e5f6",
                Before: new Dictionary<string, string?>
                {
                    ["DisplayName"] = "Ada",
                    ["passwordHash"] = "should-never-render",
                    ["ip"] = "203.0.113.10",
                    ["StrategyBody"] = "denied-field"
                },
                After: new Dictionary<string, string?>
                {
                    ["DisplayName"] = "Ada Lovelace <img>",
                    ["passwordHash"] = "should-never-render",
                    ["ip"] = "203.0.113.10"
                }),
            new(
                DeleteId,
                new DateTimeOffset(2026, 10, 5, 22, 10, 0, TimeSpan.Zero),
                Actor,
                "entity_delete",
                "Offer",
                Guid.Parse("77777777-7777-7777-7777-777777777777"),
                ReasonClass: null,
                EntityExists: true,
                IpHmacPrefix: "b2c3d4e5f6a1",
                Before: new Dictionary<string, string?> { ["Amount"] = "10.00", ["Currency"] = "USD" },
                After: new Dictionary<string, string?> { ["DeletedAt"] = "2026-10-05T22:10:00Z" }),
            new(
                AuthFailId,
                new DateTimeOffset(2026, 10, 5, 21, 0, 0, TimeSpan.Zero),
                "anonymous",
                "login_failure",
                null,
                null,
                ReasonClass: "bad_password",
                EntityExists: false,
                IpHmacPrefix: "c3d4e5f6a1b2",
                Before: null,
                After: null),
            new(
                DeletedEntityAuditId,
                new DateTimeOffset(2026, 10, 4, 18, 0, 0, TimeSpan.Zero),
                Actor,
                "entity_delete",
                "Participant",
                DeletedParticipantId,
                ReasonClass: null,
                EntityExists: false,
                IpHmacPrefix: "d4e5f6a1b2c3",
                Before: new Dictionary<string, string?> { ["DisplayName"] = "Retired" },
                After: new Dictionary<string, string?> { ["DeletedAt"] = "2026-10-04T18:00:00Z" })
        };

        for (var i = 0; i < 60; i++)
        {
            rows.Add(new StubEntry(
                Guid.Parse($"aaaaaaa1-0000-0000-0000-{i:D12}"),
                new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero).AddMinutes(-i),
                Actor,
                "login_success",
                null,
                null,
                ReasonClass: null,
                EntityExists: false,
                IpHmacPrefix: "e5f6a1b2c3d4",
                Before: null,
                After: null));
        }

        return rows;
    }

    public sealed record StubEntry(
        Guid Id,
        DateTimeOffset Timestamp,
        string Actor,
        string Action,
        string? EntityType,
        Guid? EntityId,
        string? ReasonClass,
        bool EntityExists,
        string IpHmacPrefix,
        Dictionary<string, string?>? Before,
        Dictionary<string, string?>? After,
        Guid? CorrelationId = null);

    private sealed record ParsedQuery(
        int Offset,
        int Limit,
        string Sort,
        string? Action,
        string? ActorEmail,
        string? EntityType,
        Guid? EntityId,
        string? ReasonClass,
        Guid? CorrelationId,
        DateTimeOffset? From,
        DateTimeOffset? To,
        string? Q);
}

public sealed class AdminAuditApiStubStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (context, nxt) =>
            {
                var original = context.Response.Body;
                await using var buffer = new MemoryStream();
                context.Response.Body = buffer;
                await nxt();
                var isAuditApi = context.Request.Path.StartsWithSegments("/admin/api/audit");
                var isCoreOwner = context.User.IsInRole(AdminSessionMiddleware.CoreOwnerRole);
                var isGet = HttpMethods.IsGet(context.Request.Method);
                var isMutate = HttpMethods.IsPost(context.Request.Method)
                    || HttpMethods.IsPut(context.Request.Method)
                    || HttpMethods.IsPatch(context.Request.Method)
                    || HttpMethods.IsDelete(context.Request.Method);
                var isListPath = context.Request.Path.Equals("/admin/api/audit", StringComparison.OrdinalIgnoreCase);
                if (isAuditApi && isCoreOwner
                    && ((isGet && context.Response.StatusCode == StatusCodes.Status404NotFound)
                        || (isMutate && isListPath)))
                {
                    context.Response.Body = original;
                    context.Response.Headers.ContentLength = null;
                    await AdminAuditApiStub.WriteAsync(context);
                    return;
                }

                buffer.Position = 0;
                context.Response.Body = original;
                await buffer.CopyToAsync(original);
            });
            next(app);
        };
    }
}
