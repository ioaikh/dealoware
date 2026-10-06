using System.Text.Json;
using Dealoware.Api.Admin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Dealoware.Api.Tests;

/// <summary>
/// Test-only GET /admin/api/audit stub. Step 8 owns the real endpoint.
/// Matches the locked read contract: filters (action, entityType, from, to),
/// paging (offset/limit default 50 max 200), list fields, and detail snapshots.
/// </summary>
public static class AdminAuditApiStub
{
    public static readonly Guid EditId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DeleteId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid AuthFailId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid DeletedEntityAuditId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid ExistingParticipantId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid DeletedParticipantId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid MissingId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public const string Actor = "io@aiknowhow.com";

    public static IReadOnlyList<StubEntry> Seed { get; } = CreateSeed();

    public static async Task WriteAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
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

    private static async Task WriteListAsync(HttpContext context)
    {
        var query = context.Request.Query;
        if (!TryParsePaging(query, out var offset, out var limit, out var badRequest))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("""{"error":"Bad request"}""");
            _ = badRequest;
            return;
        }

        var action = query["action"].ToString();
        var entityType = query["entityType"].ToString();
        var actorEmail = query["actorEmail"].ToString();
        var reasonClass = query["reasonClass"].ToString();
        DateTimeOffset? from = TryParseDate(query["from"].ToString());
        DateTimeOffset? to = TryParseDate(query["to"].ToString());

        var filtered = Seed.Where(entry =>
        {
            if (!string.IsNullOrWhiteSpace(action)
                && !string.Equals(entry.Action, action, StringComparison.Ordinal))
                return false;
            if (!string.IsNullOrWhiteSpace(entityType)
                && !string.Equals(entry.EntityType, entityType, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrWhiteSpace(actorEmail)
                && !entry.Actor.Contains(actorEmail, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrWhiteSpace(reasonClass)
                && !string.Equals(entry.ReasonClass ?? "", reasonClass, StringComparison.Ordinal))
                return false;
            if (from is { } fromValue && entry.Timestamp < fromValue)
                return false;
            if (to is { } toValue && entry.Timestamp > toValue)
                return false;
            return true;
        }).OrderByDescending(entry => entry.Timestamp).ThenBy(entry => entry.Id).ToList();

        var page = filtered.Skip(offset).Take(limit).Select(ToListItem).ToList();
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            offset,
            limit,
            total = filtered.Count,
            items = page
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
        await context.Response.WriteAsync(JsonSerializer.Serialize(ToDetailItem(entry)));
    }

    private static bool TryParsePaging(IQueryCollection query, out int offset, out int limit, out bool badRequest)
    {
        offset = 0;
        limit = 50;
        badRequest = false;
        var offsetRaw = query["offset"].FirstOrDefault();
        var limitRaw = query["limit"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(offsetRaw) && !int.TryParse(offsetRaw, out offset))
        {
            badRequest = true;
            return false;
        }
        if (!string.IsNullOrWhiteSpace(limitRaw) && !int.TryParse(limitRaw, out limit))
        {
            badRequest = true;
            return false;
        }
        if (offset < 0 || limit < 0)
        {
            badRequest = true;
            return false;
        }
        if (limit == 0)
            limit = 50;
        if (limit > 200)
            limit = 200;
        return true;
    }

    private static DateTimeOffset? TryParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        return DateTimeOffset.TryParse(raw, out var value) ? value : null;
    }

    private static object ToListItem(StubEntry entry) => ToReadModel(entry);

    private static object ToDetailItem(StubEntry entry) => ToReadModel(entry);

    private static object ToReadModel(StubEntry entry) => new
    {
        id = entry.Id,
        timestamp = entry.Timestamp,
        action = entry.Action,
        actorEmail = entry.Actor,
        ipHmacPrefix = entry.IpHmacPrefix,
        entityType = entry.EntityType,
        entityId = entry.EntityId,
        reasonClass = entry.ReasonClass,
        correlationId = entry.CorrelationId,
        beforeSnapshot = SerializeSnapshot(entry.Before),
        afterSnapshot = SerializeSnapshot(entry.After)
    };

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
                var isAuditRead = HttpMethods.IsGet(context.Request.Method)
                    && context.Request.Path.StartsWithSegments("/admin/api/audit");
                var isCoreOwner = context.User.IsInRole(AdminSessionMiddleware.CoreOwnerRole);
                if (isAuditRead && isCoreOwner && context.Response.StatusCode == StatusCodes.Status404NotFound)
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
