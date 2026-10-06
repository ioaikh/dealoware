using Dealoware.Domain.Admin;
using Dealoware.Domain.Artifacts;
using Dealoware.Domain.Negotiations;
using Dealoware.Domain.Participants;
using Dealoware.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dealoware.Api.Admin;

/// <summary>
/// Local Playwright seed only. Never a product bypass route.
/// Runs when DEALOWARE_ADMIN_UI_TEST=1 in Development; fail-closed otherwise.
/// </summary>
internal static class AdminUiTestHarness
{
    public const string FlagName = "DEALOWARE_ADMIN_UI_TEST";
    public const string SessionFileEnv = "DEALOWARE_ADMIN_UI_TEST_SESSION_FILE";

    public static async Task ApplyAsync(WebApplication app)
    {
        var flag = Environment.GetEnvironmentVariable(FlagName);
        if (string.IsNullOrWhiteSpace(flag)
            || !(flag == "1" || flag.Equals("true", StringComparison.OrdinalIgnoreCase)))
            return;

        if (!app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                $"{FlagName} is not allowed outside Development.");
        }

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealowareDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (await db.AdminSessions.AnyAsync())
            return;

        var ownerEmail = app.Configuration["CoreOwner:Email"] ?? "io@aiknowhow.com";
        var session = AdminSession.Create(ownerEmail, ipHmac: "testhmac-not-an-ip");
        session.MarkTotpVerified();
        db.AdminSessions.Add(session);

        var now = DateTimeOffset.UtcNow;
        var alpha = Participant.Create("Alpha Admin Person");
        var beta = Participant.Create("Beta Admin Person");
        var suspended = Participant.Create("Suspended Admin Person");
        suspended.Deactivate();
        var deletedPerson = Participant.Create("Deleted Admin Person");
        db.Participants.AddRange(alpha, beta, suspended, deletedPerson);

        var artifact = Artifact.Create(
            alpha.Sub,
            [SubjectEntity.Create("Red Widget", "A red widget for tests")],
            "sell");
        var otherArtifact = Artifact.Create(
            beta.Sub,
            [SubjectEntity.Create("Blue Gadget", "A blue gadget for tests")],
            "buy");
        var deletedArtifact = Artifact.Create(
            alpha.Sub,
            [SubjectEntity.Create("Gone Widget", "soft-deleted")],
            "sell");
        db.Artifacts.AddRange(artifact, otherArtifact, deletedArtifact);

        var (openN, e1) = Negotiation.Create(artifact.Id, alpha.Sub, beta.Sub, "sell", "buy");
        var (closedN, e2) = Negotiation.Create(artifact.Id, alpha.Sub, beta.Sub, "sell", "buy");
        var (expiredN, e3) = Negotiation.Create(
            artifact.Id, alpha.Sub, beta.Sub, "sell", "buy",
            endsAt: now.AddMinutes(-5));
        var (deletedN, e4) = Negotiation.Create(otherArtifact.Id, alpha.Sub, beta.Sub, "sell", "buy");
        if (e1.Count + e2.Count + e3.Count + e4.Count > 0)
            throw new InvalidOperationException("Admin UI test harness could not create negotiations.");
        closedN!.Close();
        expiredN!.CheckAndApplyExpiration();
        db.Negotiations.AddRange(openN!, closedN, expiredN, deletedN!);

        var (openO, _) = Offer.Create(openN!.Id, alpha.Sub, beta.Sub, 100m, "USD", "open terms");
        var (acceptedO, _) = Offer.Create(openN.Id, beta.Sub, alpha.Sub, 200m, "USD", "accepted terms");
        acceptedO!.Accept(alpha.Sub);
        var (declinedO, _) = Offer.Create(closedN.Id, alpha.Sub, beta.Sub, 50m, "USD", "declined terms");
        declinedO!.Decline(beta.Sub);
        var (withdrawnO, _) = Offer.Create(closedN.Id, beta.Sub, alpha.Sub, 75m, "USD", "withdrawn terms");
        withdrawnO!.Withdraw(beta.Sub);
        var (supersededO, _) = Offer.Create(expiredN.Id, alpha.Sub, beta.Sub, 30m, "USD", "superseded terms");
        supersededO!.Supersede();
        var (cancelledO, _) = Offer.Create(expiredN.Id, beta.Sub, alpha.Sub, 40m, "USD", "cancelled terms");
        cancelledO!.Cancel();
        var (deletedO, _) = Offer.Create(deletedN!.Id, alpha.Sub, beta.Sub, 9m, "USD", "deleted terms");
        db.Offers.AddRange(openO!, acceptedO, declinedO, withdrawnO, supersededO, cancelledO, deletedO!);

        // Extra cancelled offers so paging UI can show 50/100/200 and a 1,234-style footer.
        for (var i = 0; i < 260; i++)
        {
            var (n, nErr) = Negotiation.Create(artifact.Id, alpha.Sub, beta.Sub, "sell", "buy");
            if (nErr.Count > 0 || n is null)
                throw new InvalidOperationException("Admin UI test harness could not page-seed a negotiation.");
            n.Close();
            db.Negotiations.Add(n);
            var (o, oErr) = Offer.Create(n.Id, alpha.Sub, beta.Sub, 10m + (i % 50), "USD", $"page-seed-{i}");
            if (oErr.Count > 0 || o is null)
                throw new InvalidOperationException("Admin UI test harness could not page-seed an offer.");
            o.Cancel();
            db.Offers.Add(o);
        }

        await db.SaveChangesAsync();

        var deletedAt = now;
        await db.Participants.Where(p => p.Id == deletedPerson.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.DeletedAt, deletedAt));
        await db.Artifacts.Where(a => a.Id == deletedArtifact.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.DeletedAt, deletedAt));
        await db.Negotiations.Where(n => n.Id == deletedN.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.DeletedAt, deletedAt));
        await db.Offers.Where(o => o.Id == deletedO!.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.DeletedAt, deletedAt));

        var older = now.AddDays(-3);
        var newer = now.AddHours(-1);
        await db.Participants.Where(p => p.Id == alpha.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.CreatedAt, older)
                .SetProperty(p => p.UpdatedAt, newer));
        await db.Participants.Where(p => p.Id == beta.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.CreatedAt, newer)
                .SetProperty(p => p.UpdatedAt, older));

        var sessionFile = Environment.GetEnvironmentVariable(SessionFileEnv)
                          ?? Path.Combine(Path.GetTempPath(), "dealoware-admin-ui-session.txt");
        await File.WriteAllTextAsync(sessionFile, session.Id.ToString("D"));
        app.Logger.LogInformation("Admin UI test harness wrote session file {Path}", sessionFile);
    }
}
