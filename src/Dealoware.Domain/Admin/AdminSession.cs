namespace Dealoware.Domain.Admin;

/// <summary>
/// Server-side admin session for CoreOwner authentication.
/// Session is HttpOnly Secure SameSite=Strict cookie scoped to admin host.
/// Idle timeout 30m sliding / absolute 8h from creation.
/// </summary>
public sealed class AdminSession
{
    /// <summary>
    /// Session identifier stored in the cookie.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The CoreOwner email this session belongs to.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// When the session was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Last activity timestamp for idle timeout (sliding 30m).
    /// </summary>
    public DateTimeOffset LastActivityAt { get; private set; }

    /// <summary>
    /// Absolute expiration time (8h from creation).
    /// </summary>
    public DateTimeOffset AbsoluteExpiresAt { get; private set; }

    /// <summary>
    /// Whether TOTP verification was completed for this session.
    /// Session is not valid until TotpVerified is true.
    /// </summary>
    public bool TotpVerified { get; private set; }

    /// <summary>
    /// Keyed HMAC-SHA256 of the client IP at session creation.
    /// Raw IP is never stored.
    /// </summary>
    public string IpHmac { get; private set; } = string.Empty;

    private AdminSession() { }

    /// <summary>
    /// Creates a new admin session with 8h absolute expiration.
    /// </summary>
    public static AdminSession Create(
        string email,
        string ipHmac,
        DateTimeOffset? now = null)
    {
        var timestamp = now ?? DateTimeOffset.UtcNow;
        return new AdminSession
        {
            Id = Guid.NewGuid(),
            Email = email,
            CreatedAt = timestamp,
            LastActivityAt = timestamp,
            AbsoluteExpiresAt = timestamp.AddHours(8),
            TotpVerified = false,
            IpHmac = ipHmac
        };
    }

    /// <summary>
    /// Updates last activity for sliding window idle timeout.
    /// </summary>
    public void UpdateActivity(DateTimeOffset? now = null)
    {
        LastActivityAt = now ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the session as TOTP-verified.
    /// </summary>
    public void MarkTotpVerified()
    {
        TotpVerified = true;
    }

    /// <summary>
    /// Checks if the session has expired (idle 30m or absolute 8h).
    /// </summary>
    public bool IsExpired(DateTimeOffset? now = null)
    {
        var timestamp = now ?? DateTimeOffset.UtcNow;
        
        // Absolute expiration (8h from creation)
        if (timestamp >= AbsoluteExpiresAt)
            return true;
        
        // Idle expiration (30m since last activity)
        if (timestamp >= LastActivityAt.AddMinutes(30))
            return true;
        
        return false;
    }

    /// <summary>
    /// Checks if the session is valid for admin operations.
    /// Must not be expired AND TOTP must be verified.
    /// </summary>
    public bool IsValid(DateTimeOffset? now = null)
    {
        return !IsExpired(now) && TotpVerified;
    }
}
