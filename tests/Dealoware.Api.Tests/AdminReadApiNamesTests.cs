using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dealoware.Api.Admin;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.FieldAcl;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Domain.Strategies;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dealoware.Api.Tests;

/// <summary>
/// T04 changes to the admin read API that the list and detail pages stand on:
/// rows carry names, not bare ids (each through the field policy), offers
/// carry their artifact, every row carries updatedAt, the default order is
/// Updated, the participant status filter filters, a status filter takes
/// several statuses, a participant list row carries no email address while
/// the detail does, and a soft-deleted record's detail needs the toggle.
/// </summary>
[Collection("WebAppTests")]
public class AdminReadApiNamesTests
{
    private const string AdminHost = "admin.core.dealoware.com";
    private const string CoreOwnerEmail = TestCoreOwner.Email;

    private readonly IsolatedWebApplicationFactory _factory;

    public AdminReadApiNamesTests(IsolatedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private sealed record Graph(
        Participant Alpha,
        Participant Beta,
        Participant Suspended,
        Participant Deleted,
        Artifact Artifact,
        Negotiation Open,
        Negotiation Closed,
        Negotiation DeletedNegotiation,
        Offer OpenOffer,
        Offer AcceptedOffer,
        Offer DeclinedOffer,
        Offer DeletedOffer);

    private async Task<Graph> SeedAsync(string prefix)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();

        var alpha = Participant.Create($"{prefix}-Alpha");
        alpha.UpdateLoginEmail($"{prefix}-login@example.test");
        alpha.UpdateContactEmail($"{prefix}-contact@example.test");
        var beta = Participant.Create($"{prefix}-Beta");
        var suspended = Participant.Create($"{prefix}-Sleeper");
        suspended.Deactivate();
        var deleted = Participant.Create($"{prefix}-Gone");
        db.Participants.AddRange(alpha, beta, suspended, deleted);

        var artifact = Artifact.Create(alpha.Sub, [SubjectEntity.Create($"{prefix}-Widget", "line one\nline two")], "sell");
        db.Artifacts.Add(artifact);

        var (open, _) = Negotiation.Create(artifact.Id, alpha.Sub, beta.Sub, "sell", "buy");
        var (closed, _) = Negotiation.Create(artifact.Id, alpha.Sub, beta.Sub, "sell", "buy");
        closed!.Close();
        var (deletedNegotiation, _) = Negotiation.Create(artifact.Id, alpha.Sub, beta.Sub, "sell", "buy");
        db.Negotiations.AddRange(open!, closed, deletedNegotiation!);

        var (openOffer, _) = Offer.Create(open!.Id, alpha.Sub, beta.Sub, 10m, "USD", $"{prefix}-terms");
        var (accepted, _) = Offer.Create(open.Id, beta.Sub, alpha.Sub, 20m, "USD", $"{prefix}-accepted");
        accepted!.Accept(alpha.Sub);
        var (declined, _) = Offer.Create(closed.Id, alpha.Sub, beta.Sub, 30m, "USD", $"{prefix}-declined");
        declined!.Decline(beta.Sub);
        var (deletedOffer, _) = Offer.Create(open.Id, alpha.Sub, beta.Sub, 40m, "USD", $"{prefix}-deleted");
        db.Offers.AddRange(openOffer!, accepted, declined, deletedOffer!);

        var (strategy, _) = Strategy.Create(alpha.Sub, $"{prefix}-strat", $"{prefix}-STRATEGY-TEXT");
        db.Strategies.Add(strategy!);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        await db.Participants.Where(p => p.Id == deleted.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAt, now));
        await db.Negotiations.Where(n => n.Id == deletedNegotiation.Id).ExecuteUpdateAsync(s => s.SetProperty(n => n.DeletedAt, now));
        await db.Offers.Where(o => o.Id == deletedOffer!.Id).ExecuteUpdateAsync(s => s.SetProperty(o => o.DeletedAt, now));

        return new Graph(alpha, beta, suspended, deleted, artifact, open, closed, deletedNegotiation, openOffer!, accepted, declined, deletedOffer!);
    }

    private async Task<Guid> SessionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        var session = AdminSession.Create(CoreOwnerEmail, ipHmac: "testhmac-not-an-ip");
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private async Task<(HttpStatusCode Status, JsonElement Json, string Body)> GetAsync(string path, Guid session)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = AdminHost;
        request.Headers.TryAddWithoutValidation("Cookie", $"{AdminSessionCookie.Name}={session:D}");
        using var response = await _factory.CreateClient().SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        var json = body.StartsWith('{') ? JsonSerializer.Deserialize<JsonElement>(body) : default;
        return (response.StatusCode, json, body);
    }

    private static List<JsonElement> Items(JsonElement page) => page.GetProperty("items").EnumerateArray().ToList();

    private static JsonElement Row(JsonElement page, Guid id)
        => Items(page).Single(i => i.GetProperty("id").GetGuid() == id);

    [Fact]
    public async Task NegotiationAndOfferRows_CarryNames_AndOffersTheirArtifact()
    {
        var prefix = $"n1-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        var negotiations = await GetAsync($"/admin/api/negotiations?artifact={g.Artifact.Id:D}", session);
        Assert.Equal(HttpStatusCode.OK, negotiations.Status);
        var n = Row(negotiations.Json, g.Open.Id);
        Assert.Equal(g.Artifact.Id, n.GetProperty("artifactId").GetGuid());
        Assert.Equal($"{prefix}-Widget", n.GetProperty("artifactName").GetString());
        Assert.Equal($"{prefix}-Alpha", n.GetProperty("partyAName").GetString());
        Assert.Equal($"{prefix}-Beta", n.GetProperty("partyBName").GetString());
        Assert.True(n.TryGetProperty("updatedAt", out _));

        var offers = await GetAsync($"/admin/api/offers?negotiationId={g.Open.Id:D}", session);
        var o = Row(offers.Json, g.AcceptedOffer.Id);
        Assert.Equal(g.Artifact.Id, o.GetProperty("artifactId").GetGuid());
        Assert.Equal($"{prefix}-Widget", o.GetProperty("artifactName").GetString());
        Assert.Equal($"{prefix}-Beta", o.GetProperty("fromParticipantName").GetString());
        Assert.Equal($"{prefix}-Alpha", o.GetProperty("toParticipantName").GetString());
        Assert.True(o.TryGetProperty("updatedAt", out _));

        var artifacts = await GetAsync($"/admin/api/artifacts?q={prefix}-Widget", session);
        var a = Row(artifacts.Json, g.Artifact.Id);
        Assert.Equal($"{prefix}-Alpha", a.GetProperty("ownerName").GetString());
        Assert.True(a.TryGetProperty("updatedAt", out _));

        // The detail of each carries the same names.
        var offer = await GetAsync($"/admin/api/offers/{g.OpenOffer.Id:D}", session);
        Assert.Equal($"{prefix}-Widget", offer.Json.GetProperty("artifactName").GetString());
        Assert.Equal($"{prefix}-Alpha", offer.Json.GetProperty("fromParticipantName").GetString());
        var negotiation = await GetAsync($"/admin/api/negotiations/{g.Open.Id:D}", session);
        Assert.Equal($"{prefix}-Beta", negotiation.Json.GetProperty("partyBName").GetString());
        foreach (var body in new[] { negotiations.Body, offers.Body, artifacts.Body, offer.Body, negotiation.Body })
            Assert.DoesNotContain("STRATEGY-TEXT", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParticipantListRows_CarryNoEmail_TheDetailDoes()
    {
        var prefix = $"n2-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        var list = await GetAsync($"/admin/api/participants?q={prefix}-Alpha", session);
        var row = Row(list.Json, g.Alpha.Id);
        Assert.False(row.TryGetProperty("loginEmail", out _));
        Assert.False(row.TryGetProperty("contactEmail", out _));
        Assert.DoesNotContain("@example.test", list.Body, StringComparison.Ordinal);
        Assert.True(row.TryGetProperty("updatedAt", out _));

        var detail = await GetAsync($"/admin/api/participants/{g.Alpha.Id:D}", session);
        Assert.Equal($"{prefix}-login@example.test", detail.Json.GetProperty("loginEmail").GetString());
        Assert.Equal($"{prefix}-contact@example.test", detail.Json.GetProperty("contactEmail").GetString());
    }

    [Theory]
    [InlineData("Active", true, false)]
    [InlineData("suspended", false, true)]
    [InlineData("Active,Suspended", true, true)]
    public async Task ParticipantStatusFilter_Filters(string status, bool expectActive, bool expectSuspended)
    {
        var prefix = $"n3-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        var page = await GetAsync($"/admin/api/participants?q={prefix}-&status={status}", session);
        Assert.Equal(HttpStatusCode.OK, page.Status);
        var ids = Items(page.Json).Select(i => i.GetProperty("id").GetGuid()).ToList();
        Assert.Equal(expectActive, ids.Contains(g.Alpha.Id));
        Assert.Equal(expectSuspended, ids.Contains(g.Suspended.Id));
        Assert.All(Items(page.Json), i => Assert.True(
            i.GetProperty("isActive").GetBoolean() ? expectActive : expectSuspended));
    }

    [Theory]
    [InlineData("/admin/api/participants?status=Deleted")]
    [InlineData("/admin/api/participants?status=Active,1")]
    [InlineData("/admin/api/negotiations?status=Withdrawn")]
    [InlineData("/admin/api/negotiations?status=Open,Accepted")]
    [InlineData("/admin/api/negotiations?status=1")]
    [InlineData("/admin/api/offers?status=Open,Deleted")]
    [InlineData("/admin/api/offers?status=99")]
    [InlineData("/admin/api/offers?status=Open,,Closed")]
    public async Task AStatusTheListDoesNotHave_KeepsTheGeneric400(string path)
    {
        var session = await SessionAsync();
        var reply = await GetAsync(path, session);
        Assert.Equal(HttpStatusCode.BadRequest, reply.Status);
        Assert.Equal("{\"error\":\"BadRequest\"}", reply.Body);
    }

    [Fact]
    public async Task AStatusFilter_TakesSeveralStatuses()
    {
        var prefix = $"n4-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        var negotiations = await GetAsync($"/admin/api/negotiations?artifact={g.Artifact.Id:D}&status=open,CLOSED", session);
        Assert.Equal(HttpStatusCode.OK, negotiations.Status);
        Assert.Equal(
            new[] { g.Open.Id, g.Closed.Id }.OrderBy(x => x),
            Items(negotiations.Json).Select(i => i.GetProperty("id").GetGuid()).OrderBy(x => x));

        var offers = await GetAsync($"/admin/api/offers?artifact={g.Artifact.Id:D}&status=Accepted,Declined", session);
        Assert.Equal(
            new[] { g.AcceptedOffer.Id, g.DeclinedOffer.Id }.OrderBy(x => x),
            Items(offers.Json).Select(i => i.GetProperty("id").GetGuid()).OrderBy(x => x));

        var one = await GetAsync($"/admin/api/offers?artifact={g.Artifact.Id:D}&status=Accepted", session);
        Assert.Equal(g.AcceptedOffer.Id, Assert.Single(Items(one.Json)).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task TheDefaultOrder_IsUpdatedNewestFirst()
    {
        var prefix = $"n5-{Guid.NewGuid():N}";
        var older = Participant.Create($"{prefix}-CreatedLater");
        var newer = Participant.Create($"{prefix}-UpdatedLater");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            db.Participants.AddRange(older, newer);
            await db.SaveChangesAsync();
            var t = DateTimeOffset.UtcNow;
            // Created later, never updated; created earlier, updated last.
            await db.Participants.Where(p => p.Id == older.Id).ExecuteUpdateAsync(s => s
                .SetProperty(p => p.CreatedAt, t.AddDays(-1))
                .SetProperty(p => p.UpdatedAt, (DateTimeOffset?)null));
            await db.Participants.Where(p => p.Id == newer.Id).ExecuteUpdateAsync(s => s
                .SetProperty(p => p.CreatedAt, t.AddDays(-3))
                .SetProperty(p => p.UpdatedAt, t));
        }

        var session = await SessionAsync();
        var page = await GetAsync($"/admin/api/participants?q={prefix}-", session);
        Assert.Equal(
            new[] { newer.Id, older.Id },
            Items(page.Json).Select(i => i.GetProperty("id").GetGuid()).ToArray());
        Assert.Equal(AdminListQuery.DefaultSort, new AdminListQuery().Sort);
        Assert.Equal("updated", AdminListQuery.DefaultSort);
    }

    [Fact]
    public async Task ASoftDeletedRecordsDetail_NeedsTheToggle()
    {
        var prefix = $"n6-{Guid.NewGuid():N}";
        var g = await SeedAsync(prefix);
        var session = await SessionAsync();

        foreach (var path in new[]
                 {
                     $"/admin/api/participants/{g.Deleted.Id:D}",
                     $"/admin/api/negotiations/{g.DeletedNegotiation.Id:D}",
                     $"/admin/api/offers/{g.DeletedOffer.Id:D}"
                 })
        {
            var hidden = await GetAsync(path, session);
            Assert.Equal(HttpStatusCode.NotFound, hidden.Status);
            Assert.Equal("{\"error\":\"NotFound\"}", hidden.Body);

            var shown = await GetAsync(path + "?includeDeleted=true", session);
            Assert.Equal(HttpStatusCode.OK, shown.Status);
            Assert.NotEqual(JsonValueKind.Null, shown.Json.GetProperty("deletedAt").ValueKind);

            var bad = await GetAsync(path + "?includeDeleted=maybe", session);
            Assert.Equal(HttpStatusCode.BadRequest, bad.Status);
        }

        // A record that is not deleted is there either way.
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/admin/api/offers/{g.OpenOffer.Id:D}", session)).Status);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/admin/api/offers/{g.OpenOffer.Id:D}?includeDeleted=true", session)).Status);
    }

    [Fact]
    public async Task ADeletedRecordsName_ShowsOnlyWithTheToggle_ItsDeletedMarkAlways()
    {
        var prefix = $"n7-{Guid.NewGuid():N}";
        var alpha = Participant.Create($"{prefix}-Alpha");
        var gone = Participant.Create($"{prefix}-GonePerson");
        var goneThing = Artifact.Create(gone.Sub, [SubjectEntity.Create($"{prefix}-GoneThing", "d")], "sell");
        var (live, _) = Negotiation.Create(goneThing.Id, alpha.Sub, gone.Sub, "sell", "buy");
        var (goneNegotiation, _) = Negotiation.Create(goneThing.Id, alpha.Sub, gone.Sub, "sell", "buy");
        var (liveOffer, _) = Offer.Create(live!.Id, gone.Sub, alpha.Sub, 5m, "USD", "t");
        var (orphanOffer, _) = Offer.Create(goneNegotiation!.Id, alpha.Sub, gone.Sub, 6m, "USD", "t");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
            db.Participants.AddRange(alpha, gone);
            db.Artifacts.Add(goneThing);
            db.Negotiations.AddRange(live, goneNegotiation);
            db.Offers.AddRange(liveOffer!, orphanOffer!);
            await db.SaveChangesAsync();
            var at = DateTimeOffset.UtcNow;
            await db.Participants.Where(p => p.Id == gone.Id).ExecuteUpdateAsync(u => u.SetProperty(p => p.DeletedAt, at));
            await db.Artifacts.Where(a => a.Id == goneThing.Id).ExecuteUpdateAsync(u => u.SetProperty(a => a.DeletedAt, at));
            await db.Negotiations.Where(n => n.Id == goneNegotiation.Id).ExecuteUpdateAsync(u => u.SetProperty(n => n.DeletedAt, at));
        }

        var session = await SessionAsync();
        foreach (var toggle in new[] { false, true })
        {
            var on = toggle ? "&includeDeleted=true" : string.Empty;
            var n = Row((await GetAsync($"/admin/api/negotiations?negotiationId={live.Id:D}{on}", session)).Json, live.Id);
            Assert.True(n.GetProperty("artifactDeleted").GetBoolean());
            Assert.True(n.GetProperty("partyBDeleted").GetBoolean());
            Assert.False(n.TryGetProperty("partyADeleted", out _));
            Assert.Equal($"{prefix}-Alpha", n.GetProperty("partyAName").GetString());
            Assert.Equal(toggle, n.TryGetProperty("artifactName", out _));
            Assert.Equal(toggle, n.TryGetProperty("partyBName", out _));

            var offers = await GetAsync($"/admin/api/offers?participant={Uri.EscapeDataString(alpha.Sub)}{on}", session);
            var o = Row(offers.Json, liveOffer!.Id);
            Assert.True(o.GetProperty("fromParticipantDeleted").GetBoolean());
            Assert.Equal(toggle, o.TryGetProperty("fromParticipantName", out _));
            Assert.True(o.GetProperty("artifactDeleted").GetBoolean());
            Assert.False(o.TryGetProperty("negotiationDeleted", out _));
            var orphan = Row(offers.Json, orphanOffer!.Id);
            Assert.True(orphan.GetProperty("negotiationDeleted").GetBoolean());

            var detail = await GetAsync($"/admin/api/negotiations/{live.Id:D}{(toggle ? "?includeDeleted=true" : string.Empty)}", session);
            Assert.Equal(toggle, detail.Json.TryGetProperty("artifactName", out _));
            Assert.Equal(toggle, detail.Json.TryGetProperty("partyBName", out _));
            foreach (var body in new[] { offers.Body, detail.Body })
            {
                Assert.Equal(toggle, body.Contains($"{prefix}-GonePerson", StringComparison.Ordinal));
                Assert.Equal(toggle, body.Contains($"{prefix}-GoneThing", StringComparison.Ordinal));
            }
        }
    }

    [Fact]
    public void ADeletedReference_InTheProjection_GivesItsMark_AndItsNameOnlyWithTheToggle()
    {
        var (a, b, artifact, negotiation, offer, related) = Rows();
        var owner = FieldPrincipal.CoreOwner(CoreOwnerEmail);
        typeof(Participant).GetProperty(nameof(Participant.DeletedAt))!.SetValue(b, DateTimeOffset.UtcNow);
        typeof(Artifact).GetProperty(nameof(Artifact.DeletedAt))!.SetValue(artifact, DateTimeOffset.UtcNow);

        var off = AdminReadProjection.Negotiation(negotiation, new FieldPolicy(), owner, detail: false, related);
        Assert.Equal(true, off["partyBDeleted"]);
        Assert.False(off.ContainsKey("partyBName"));
        Assert.Equal(true, off["artifactDeleted"]);
        Assert.False(off.ContainsKey("artifactName"));
        Assert.Equal("Name-A", off["partyAName"]);

        var on = AdminReadProjection.Negotiation(negotiation, new FieldPolicy(), owner, detail: false, related with { IncludeDeleted = true });
        Assert.Equal("Name-B", on["partyBName"]);
        Assert.Equal("Name-Artifact", on["artifactName"]);
        Assert.Equal(true, on["partyBDeleted"]);

        // The mark is the soft-delete marker: denied, no mark (and still no name).
        var unmarked = AdminReadProjection.Offer(offer, new DenyingPolicy(FieldClass.SoftDeletedAt), owner, detail: false, related);
        Assert.False(unmarked.ContainsKey("toParticipantDeleted"));
        Assert.False(unmarked.ContainsKey("toParticipantName"));
        Assert.Equal("Name-A", unmarked["fromParticipantName"]);
        _ = a;
    }

    // ---- the field policy over the joined names ----

    /// <summary>The real policy, with the named field classes denied.</summary>
    private sealed class DenyingPolicy(params FieldClass[] denied) : IFieldPolicy
    {
        private readonly FieldPolicy _inner = new();

        public bool IsRegistered(FieldClass fieldClass) => _inner.IsRegistered(fieldClass);

        public bool Evaluate(FieldPrincipal principal, FieldClass fieldClass, FieldAction action, FieldResourceContext resourceContext)
            => !denied.Contains(fieldClass) && _inner.Evaluate(principal, fieldClass, action, resourceContext);
    }

    private static (Participant A, Participant B, Artifact Artifact, Negotiation Negotiation, Offer Offer, AdminRelatedRows Related) Rows()
    {
        var a = Participant.Create("Name-A");
        var b = Participant.Create("Name-B");
        var artifact = Artifact.Create(a.Sub, [SubjectEntity.Create("Name-Artifact", "d")], "sell");
        var (negotiation, _) = Negotiation.Create(artifact.Id, a.Sub, b.Sub, "sell", "buy");
        var (offer, _) = Offer.Create(negotiation!.Id, a.Sub, b.Sub, 1m, "USD", "t");
        var related = new AdminRelatedRows(
            new Dictionary<string, Participant>(StringComparer.Ordinal) { [a.Sub] = a, [b.Sub] = b },
            new Dictionary<Guid, Artifact> { [artifact.Id] = artifact },
            new Dictionary<Guid, Negotiation> { [negotiation.Id] = negotiation });
        return (a, b, artifact, negotiation, offer!, related);
    }

    [Fact]
    public void JoinedNames_PassTheFieldPolicy_AsTheirOwnFields()
    {
        var (_, _, artifact, negotiation, offer, related) = Rows();
        var owner = FieldPrincipal.CoreOwner(CoreOwnerEmail);

        // The real policy: every name is there.
        var all = new FieldPolicy();
        var n = AdminReadProjection.Negotiation(negotiation, all, owner, detail: false, related);
        Assert.Equal("Name-Artifact", n["artifactName"]);
        Assert.Equal("Name-A", n["partyAName"]);
        Assert.Equal("Name-B", n["partyBName"]);
        var o = AdminReadProjection.Offer(offer, all, owner, detail: false, related);
        Assert.Equal(artifact.Id, o["artifactId"]);
        Assert.Equal("Name-Artifact", o["artifactName"]);
        Assert.Equal("Name-A", o["fromParticipantName"]);
        Assert.Equal("Name-B", o["toParticipantName"]);
        Assert.Equal("Name-A", AdminReadProjection.Artifact(artifact, all, owner, detail: false, related)["ownerName"]);

        // Display names denied: no participant name anywhere, the ids stay.
        var noNames = new DenyingPolicy(FieldClass.DisplayName);
        n = AdminReadProjection.Negotiation(negotiation, noNames, owner, detail: true, related);
        Assert.False(n.ContainsKey("partyAName"));
        Assert.False(n.ContainsKey("partyBName"));
        Assert.True(n.ContainsKey("partyAParticipantId"));
        o = AdminReadProjection.Offer(offer, noNames, owner, detail: false, related);
        Assert.False(o.ContainsKey("fromParticipantName"));
        Assert.False(o.ContainsKey("toParticipantName"));
        Assert.False(AdminReadProjection.Artifact(artifact, noNames, owner, detail: false, related).ContainsKey("ownerName"));

        // Artifact names denied: no artifact name, the artifact id stays.
        var noArtifactNames = new DenyingPolicy(FieldClass.ArtifactName);
        Assert.False(AdminReadProjection.Negotiation(negotiation, noArtifactNames, owner, detail: false, related).ContainsKey("artifactName"));
        o = AdminReadProjection.Offer(offer, noArtifactNames, owner, detail: false, related);
        Assert.False(o.ContainsKey("artifactName"));
        Assert.Equal(artifact.Id, o["artifactId"]);

        // The reference denied: neither the id nor the name it would lead to.
        n = AdminReadProjection.Negotiation(negotiation, new DenyingPolicy(FieldClass.NegotiationArtifactId), owner, detail: false, related);
        Assert.False(n.ContainsKey("artifactId"));
        Assert.False(n.ContainsKey("artifactName"));
        o = AdminReadProjection.Offer(offer, new DenyingPolicy(FieldClass.NegotiationArtifactId), owner, detail: false, related);
        Assert.False(o.ContainsKey("artifactId"));
        Assert.False(o.ContainsKey("artifactName"));
        o = AdminReadProjection.Offer(offer, new DenyingPolicy(FieldClass.OfferNegotiationId), owner, detail: false, related);
        Assert.False(o.ContainsKey("negotiationId"));
        Assert.False(o.ContainsKey("artifactId"));
        n = AdminReadProjection.Negotiation(negotiation, new DenyingPolicy(FieldClass.NegotiationPartyA), owner, detail: false, related);
        Assert.False(n.ContainsKey("partyAParticipantId"));
        Assert.False(n.ContainsKey("partyAName"));
        Assert.Equal("Name-B", n["partyBName"]);
        var a = AdminReadProjection.Artifact(artifact, new DenyingPolicy(FieldClass.ArtifactOwnerParticipantId), owner, detail: false, related);
        Assert.False(a.ContainsKey("ownerName"));

        // Timestamps: updatedAt goes with createdAt.
        Assert.False(AdminReadProjection.Offer(offer, new DenyingPolicy(FieldClass.EntityCreatedAt), owner, detail: false, related).ContainsKey("updatedAt"));
    }

    [Fact]
    public void EmailAddresses_AreOnTheDetail_NeverInAListRow_AndTheDetailStillAsksThePolicy()
    {
        var p = Participant.Create("Mailer");
        p.UpdateLoginEmail("login@example.test");
        p.UpdateContactEmail("contact@example.test");
        var owner = FieldPrincipal.CoreOwner(CoreOwnerEmail);

        var row = AdminReadProjection.Participant(p, new FieldPolicy(), owner, detail: false);
        Assert.False(row.ContainsKey("loginEmail"));
        Assert.False(row.ContainsKey("contactEmail"));
        var detail = AdminReadProjection.Participant(p, new FieldPolicy(), owner, detail: true);
        Assert.Equal("login@example.test", detail["loginEmail"]);
        Assert.Equal("contact@example.test", detail["contactEmail"]);
        var denied = AdminReadProjection.Participant(p, new DenyingPolicy(FieldClass.LoginEmail, FieldClass.ContactEmail), owner, detail: true);
        Assert.False(denied.ContainsKey("loginEmail"));
        Assert.False(denied.ContainsKey("contactEmail"));
    }

    [Fact]
    public void TheFieldPolicy_IsUnchanged_ForTheCoreOwner()
    {
        var policy = new FieldPolicy();
        var owner = FieldPrincipal.CoreOwner(CoreOwnerEmail);
        var ctx = FieldResourceContext.ForSelfProfile("x");
        Assert.True(policy.Evaluate(owner, FieldClass.LoginEmail, FieldAction.List, ctx));
        Assert.True(policy.Evaluate(owner, FieldClass.ContactEmail, FieldAction.List, ctx));
        Assert.False(policy.Evaluate(owner, FieldClass.StrategyBody, FieldAction.Read, ctx));
        Assert.False(policy.Evaluate(owner, FieldClass.LoginEmail, FieldAction.Write, ctx));
    }
}
