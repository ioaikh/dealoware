namespace Dealoware.Domain.Admin;

/// <summary>
/// The outcome of an audit entry, as the audit viewer's Outcome column shows
/// it (Spec §10 "Context: Outcome, failure reason class"; TD-ADM-UI-na-X02).
/// It is not stored: it is worked out from the entry's action and reason
/// class by the one table in this file.
/// <list type="bullet">
/// <item><see cref="Success"/>: what was asked for was done.</item>
/// <item><see cref="Failure"/>: the request was judged and did not pass
/// (a wrong password or code, a link that is no longer valid).</item>
/// <item><see cref="Denied"/>: the request was refused before it was judged
/// (the account was locked, the rate limit was reached, or the security
/// check did not pass), and the start of a lock itself.</item>
/// </list>
/// The table also is the fixed list of action names the audit read API's
/// action filter takes (<see cref="Actions"/>): every action the app writes.
/// </summary>
public static class AdminAuditOutcome
{
    public const string Success = "success";
    public const string Failure = "failure";
    public const string Denied = "denied";

    /// <summary>
    /// Every action the app writes, in the order the viewer lists them, with
    /// its outcome when the entry carries no reason class.
    /// </summary>
    private static readonly (string Action, string Outcome)[] ByAction =
    [
        (AdminAuditActions.LoginSuccess, Success),
        (AdminAuditActions.LoginFailure, Failure),
        (AdminAuthAction.SecondFactorFailed, Failure),
        (AdminAuthAction.LockStart, Denied),
        (AdminAuditActions.SignOut, Success),
        (AdminAuditActions.RecoveryCodeUse, Success),
        (AdminAuditActions.ResetRequest, Success),
        (AdminAuditActions.ResetRequestFailed, Failure),
        (AdminAuditActions.ResetComplete, Success),
        (AdminAuditActions.ResetCompleteFailed, Failure),
        (AdminAuditActions.TotpEnroll, Success),
        (AdminAuditActions.TotpChange, Success),
        (AdminAuditActions.TotpChangeFailed, Failure),
        (AdminAuditActions.PasswordChanged, Success),
        (AdminAuditActions.PasswordChangeFailed, Failure),
        (AdminAuditActions.StepUpFailed, Failure),
        (AdminAuditActions.RecoveryCodesRegenerated, Success),
        (AdminAuthEvents.BootstrapLinkIssued, Success),
        (AdminAuthEvents.BootstrapComplete, Success),
        (AdminAuthEvents.BootstrapCompleteFailed, Failure),
        (AdminAuditActions.EntityEdit, Success),
        (AdminAuditActions.EntityDelete, Success)
    ];

    /// <summary>
    /// The reason classes that mean the request was refused before it was
    /// judged. Any other reason class means it was judged and failed.
    /// </summary>
    private static readonly HashSet<string> RefusedBeforeJudged = new(StringComparer.Ordinal)
    {
        AdminAuthReason.Locked,
        AdminAuthReason.RateLimited,
        AdminAuthReason.CaptchaFailed
    };

    private static readonly Dictionary<string, string> Lookup =
        ByAction.ToDictionary(row => row.Action, row => row.Outcome, StringComparer.Ordinal);

    /// <summary>The fixed list of action names, in the table's order.</summary>
    public static IReadOnlyList<string> Actions { get; } = ByAction.Select(row => row.Action).ToArray();

    public static bool IsKnownAction(string? action) => action is not null && Lookup.ContainsKey(action);

    /// <summary>
    /// The outcome of an entry. A reason class decides first: one of the
    /// three refusals is <see cref="Denied"/>, any other is
    /// <see cref="Failure"/>. Without one, the table's row for the action
    /// decides. An action the table does not name is a
    /// <see cref="Failure"/> when its name ends in <c>_failed</c> or
    /// <c>_failure</c>; otherwise it follows the entry's own rule
    /// (<see cref="AdminAuditEntry.ReasonClass"/> is null for a success).
    /// </summary>
    public static string Of(string? action, string? reasonClass)
    {
        if (!string.IsNullOrEmpty(reasonClass))
            return RefusedBeforeJudged.Contains(reasonClass) ? Denied : Failure;

        if (action is not null && Lookup.TryGetValue(action, out var outcome))
            return outcome;

        return action is not null
               && (action.EndsWith("_failed", StringComparison.Ordinal) || action.EndsWith("_failure", StringComparison.Ordinal))
            ? Failure
            : Success;
    }

    /// <summary>
    /// The reason classes of a failed step-up that no read of the log gives
    /// (<see cref="IsWithheldReason(string?, string?)"/>).
    /// </summary>
    private static readonly string[] StepUpGuessReasons = [AdminAuthReason.BadPassword, AdminAuthReason.Bad2Fa];

    /// <summary>The actions whose reason class is withheld when it says which part of a step-up was wrong.</summary>
    public static IReadOnlyList<string> StepUpFailureActions { get; } =
        [AdminAuditActions.StepUpFailed, AdminAuditActions.PasswordChangeFailed];

    /// <summary>
    /// True for the reason class of a failed S-A12 step-up
    /// (<c>account.step_up_failed</c>, <c>account.password_change_failed</c>)
    /// that says whether the password or the code was wrong. The log is read
    /// with the same session that sent the guess, so showing it would tell a
    /// holder of a stolen session cookie that a guessed password was right
    /// (<c>bad_2fa</c> means the password passed). The stored row keeps its
    /// reason; no read gives it, and no reason filter finds the row by it.
    /// The outcome is <see cref="Failure"/> either way, so it tells nothing.
    /// </summary>
    public static bool IsWithheldReason(string? action, string? reasonClass)
        => action is not null
           && reasonClass is not null
           && StepUpFailureActions.Contains(action, StringComparer.Ordinal)
           && StepUpGuessReasons.Contains(reasonClass, StringComparer.Ordinal);

    /// <summary>True for a reason class that is withheld on some action: a filter by it leaves those actions out.</summary>
    public static bool IsStepUpGuessReason(string? reasonClass)
        => reasonClass is not null && StepUpGuessReasons.Contains(reasonClass, StringComparer.Ordinal);
}
