namespace Dealoware.Domain.Admin;

/// <summary>
/// S-A12 step-up held for one signed-in session (TD-ADM-174, TD-ADM-UI-auth-18):
/// the time the owner last gave the current password plus a current
/// authenticator or recovery code on that session. Re-enrolment and new
/// recovery codes are allowed only within <see cref="Window"/> of it.
/// <para>
/// One row per session, keyed by the session id and deleted with the session
/// (cascade), so it never outlives the session it was given to. It also holds
/// the authenticator key of a re-enrolment that was started and not yet
/// confirmed: encrypted, bound to this session and this step-up, and dropped
/// by the next step-up, the next start, the confirm, or the end of the session.
/// </para>
/// </summary>
public sealed class AdminSessionStepUp
{
    /// <summary>How long one step-up unlocks re-enrolment and new codes ("within 5 minutes").</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    public Guid SessionId { get; private set; }

    /// <summary>When the current password and a current code were last accepted on this session.</summary>
    public DateTimeOffset SteppedUpAt { get; private set; }

    /// <summary>
    /// Set when a new set of recovery codes was issued under this step-up.
    /// One step-up issues one set, so a repeated submit cannot issue a second.
    /// </summary>
    public DateTimeOffset? CodesIssuedAt { get; private set; }

    /// <summary>
    /// AES-GCM ciphertext (same protector as the enrolled key) of the
    /// authenticator key a re-enrolment started under this step-up. Null when
    /// none is waiting. Never returned by any read.
    /// </summary>
    public string? PendingTotpSecretCipher { get; private set; }

    private AdminSessionStepUp() { }

    public static AdminSessionStepUp Create(Guid sessionId, DateTimeOffset now)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("A step-up belongs to a session.", nameof(sessionId));

        return new AdminSessionStepUp
        {
            SessionId = sessionId,
            SteppedUpAt = now
        };
    }

    /// <summary>A new step-up: a fresh window, no set issued under it, no key waiting.</summary>
    public void Renew(DateTimeOffset now)
    {
        SteppedUpAt = now;
        CodesIssuedAt = null;
        PendingTotpSecretCipher = null;
    }

    /// <summary>Within the window: the same test as the SQL cutoff (<see cref="FreshCutoff"/>).</summary>
    public bool IsFresh(DateTimeOffset now) => SteppedUpAt >= FreshCutoff(now);

    public DateTimeOffset ExpiresAt => SteppedUpAt + Window;

    /// <summary>The earliest step-up time still inside the window at <paramref name="now"/>.</summary>
    public static DateTimeOffset FreshCutoff(DateTimeOffset now) => now - Window;
}
