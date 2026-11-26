namespace Dealoware.Api.Admin;

/// <summary>
/// Copy of the signed-in console shell and of the pages built on it in T04
/// piece 1. Titles follow "{Screen} · Dealoware admin" (TD-ADM-UI-na-X03).
/// Nav labels, the skip link, S-D3's sentence and the signed-out sentence are
/// the test design's (TD-ADM-UI-na-X03, TD-ADM-179, TD-ADM-UI-auth-17); the
/// stat labels are Spec §6's counts. Sentences written new for this piece are
/// marked "New copy".
/// </summary>
public static class AdminConsoleCopy
{
    public const string TitleSuffix = " · Dealoware admin";

    /// <summary>The name above the nav. The product name the titles already carry.</summary>
    public const string Brand = "Dealoware admin";

    public const string SkipLink = "Skip to main content";

    /// <summary>The nav's accessible name. New copy.</summary>
    public const string NavLabel = "Admin";

    public const string NavStats = "Stats";
    public const string NavParticipants = "Participants";
    public const string NavArtifacts = "Artifacts";
    public const string NavNegotiations = "Negotiations";
    public const string NavOffers = "Offers";
    public const string NavSecuritySettings = "Security settings";
    public const string SignOut = "Sign out";

    // ---- S-B1 Stats (Spec §6) ----

    public const string StatsHeading = "Stats";
    public const string StatParticipants = "Participants";
    public const string StatOpenNegotiations = "Open negotiations";
    public const string StatOffers = "Offers";
    public const string StatAccepts = "Accepts";
    public const string StatDeclines = "Declines";

    /// <summary>"As of {time} ET" (TD-ADM-UI-na-B09), the time as in TD-ADM-UI-na-B15.</summary>
    public const string AsOfPrefix = "As of ";

    // ---- Pages later pieces fill ----

    /// <summary>What a page that is routed but not built yet says. New copy.</summary>
    public const string NotBuiltYet = "This page is not built yet.";

    // ---- S-D3 ----

    /// <summary>S-D3's title. New copy.</summary>
    public const string NotFoundTitle = "Page not found" + TitleSuffix;

    public const string NotFoundHeading = "We can't find that page.";

    /// <summary>S-D3's link to Stats. New copy.</summary>
    public const string GoToStats = "Go to Stats";

    public static string Title(string screen) => screen + TitleSuffix;
}
