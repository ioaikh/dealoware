namespace Dealoware.Domain.Admin;

/// <summary>
/// The recovery-code banner of one signed-in session (UXR-A40,
/// TD-ADM-UI-auth-16): the session was opened with a recovery code at sign-in
/// step 2, and the owner has not dismissed the banner yet.
/// <para>
/// One row per session, keyed by the session id and deleted with the session
/// (cascade), so the banner never shows in a later session. Written in the same
/// save as the session it belongs to; deleted when the owner dismisses it. It
/// holds no code and no count: the signed-in pages read the count of unused
/// codes from the store each time they show the banner.
/// </para>
/// </summary>
public sealed class AdminSessionRecoveryNotice
{
    public Guid SessionId { get; private set; }

    /// <summary>When the session was opened with a recovery code.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    private AdminSessionRecoveryNotice() { }

    public static AdminSessionRecoveryNotice Create(Guid sessionId, DateTimeOffset now)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("A recovery-code notice belongs to a session.", nameof(sessionId));

        return new AdminSessionRecoveryNotice
        {
            SessionId = sessionId,
            CreatedAt = now
        };
    }
}
