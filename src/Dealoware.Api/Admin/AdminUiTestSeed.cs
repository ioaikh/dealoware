using System.Text.Json;
using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Persistence;

namespace Dealoware.Api.Admin;

/// <summary>
/// Local Playwright host only. Never enabled unless ADMIN_UI_TEST_SEED=1.
/// Writes a seed manifest for specs. No secrets.
/// </summary>
public static class AdminUiTestSeed
{
    public static void TrySeed(WebApplication app)
    {
        var enabled = app.Configuration["ADMIN_UI_TEST_SEED"]
                      ?? Environment.GetEnvironmentVariable("ADMIN_UI_TEST_SEED");
        if (!string.Equals(enabled, "1", StringComparison.Ordinal))
            return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        if (db.AdminSessions.Any())
            return;

        var alice = Participant.Create("Alice Example");
        var bob = Participant.Create("Bob Example");
        var carol = Participant.Create("Carol Cascade");
        db.Participants.AddRange(alice, bob, carol);

        var lamp = Artifact.Create(alice.Sub, [SubjectEntity.Create("Vintage lamp", "Brass")], "sell");
        var blocked = Artifact.Create(alice.Sub, [SubjectEntity.Create("Blocked artifact", "In use")], "sell");
        var lonely = Artifact.Create(bob.Sub, [SubjectEntity.Create("Lonely artifact", "No talks")], "sell");
        db.Artifacts.AddRange(lamp, blocked, lonely);

        var (openNeg, err1) = Negotiation.Create(lamp.Id, alice.Sub, bob.Sub, "sell", "buy");
        var (blockNeg1, err2) = Negotiation.Create(blocked.Id, alice.Sub, bob.Sub, "sell", "buy");
        var (blockNeg2, err3) = Negotiation.Create(blocked.Id, alice.Sub, carol.Sub, "sell", "buy");
        var (cascadeNeg, err4) = Negotiation.Create(lamp.Id, carol.Sub, bob.Sub, "sell", "buy");
        if (openNeg is null || blockNeg1 is null || blockNeg2 is null || cascadeNeg is null)
            throw new InvalidOperationException("Admin UI test seed failed to create negotiations.");
        _ = err1; _ = err2; _ = err3; _ = err4;
        db.Negotiations.AddRange(openNeg, blockNeg1, blockNeg2, cascadeNeg);

        var (offer1, _) = Offer.Create(openNeg.Id, alice.Sub, bob.Sub, 10m, "USD", "first");
        var (offer2, _) = Offer.Create(cascadeNeg.Id, carol.Sub, bob.Sub, 20m, "USD", "cascade");
        var (offer3, _) = Offer.Create(openNeg.Id, bob.Sub, alice.Sub, 12m, "USD", "counter");
        if (offer1 is null || offer2 is null || offer3 is null)
            throw new InvalidOperationException("Admin UI test seed failed to create offers.");
        // one-open-per-side: supersede first before adding second on same negotiation
        offer1.Supersede();
        db.Offers.AddRange(offer1, offer2, offer3);

        var session = AdminSession.Create("io@aiknowhow.com", "testhmac-not-an-ip");
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);
        db.SaveChanges();

        var manifest = new Dictionary<string, string>
        {
            ["sessionId"] = session.Id.ToString("D"),
            ["aliceId"] = alice.Id.ToString("D"),
            ["aliceName"] = "Alice Example",
            ["carolId"] = carol.Id.ToString("D"),
            ["carolName"] = "Carol Cascade",
            ["lampId"] = lamp.Id.ToString("D"),
            ["lampName"] = "Vintage lamp",
            ["blockedArtifactId"] = blocked.Id.ToString("D"),
            ["lonelyArtifactId"] = lonely.Id.ToString("D"),
            ["lonelyName"] = "Lonely artifact",
            ["openNegotiationId"] = openNeg.Id.ToString("D"),
            ["blockNeg1Id"] = blockNeg1.Id.ToString("D"),
            ["blockNeg2Id"] = blockNeg2.Id.ToString("D"),
            ["cascadeNegotiationId"] = cascadeNeg.Id.ToString("D"),
            ["offerId"] = offer3.Id.ToString("D"),
            ["offer2Id"] = offer2.Id.ToString("D")
        };

        var path = app.Configuration["ADMIN_UI_TEST_SEED_FILE"]
                   ?? Environment.GetEnvironmentVariable("ADMIN_UI_TEST_SEED_FILE")
                   ?? Path.Combine(Path.GetTempPath(), "dealoware-admin-ui-seed.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(manifest));
    }
}
