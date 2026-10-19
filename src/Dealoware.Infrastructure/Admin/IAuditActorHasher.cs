namespace Dealoware.Infrastructure.Admin;

/// <summary>
/// HMAC of a normalized email for audit ActorEmail. Uses a key derived
/// from DEALOWARE_ADMIN_IP_HMAC_KEY so the actor never equals a lock or
/// failure SubjectKey.
/// </summary>
public interface IAuditActorHasher
{
    /// <summary>
    /// Base64 HMAC-SHA256 of <paramref name="normalizedEmail"/> under the
    /// HKDF-derived audit key.
    /// </summary>
    string HashEmail(string normalizedEmail);
}
