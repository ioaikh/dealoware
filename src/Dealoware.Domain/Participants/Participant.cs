namespace Dealoware.Domain.Participants;

/// <summary>
/// Participant principal with OIDC-shaped claims.
/// Maps to Artifact.OwnerParticipantId via the Sub (subject) claim.
/// 
/// OIDC Claim Mapping:
/// - Sub: Stable subject identifier (unique, immutable) - maps to Artifact.OwnerParticipantId
/// - Iss: Issuer identifier (always "dealoware" for self-issued)
/// - Aud: Audience (always "dealoware-api" for this platform)
/// - Iat: Issued at timestamp
/// - Exp: Expiration timestamp
/// </summary>
public sealed class Participant
{
    /// <summary>
    /// Internal database identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// OIDC 'sub' claim - stable subject identifier.
    /// This is the principal identifier used across the platform.
    /// Maps directly to Artifact.OwnerParticipantId.
    /// Format: "participant:{guid}" for self-registered participants.
    /// </summary>
    public string Sub { get; private set; } = string.Empty;

    /// <summary>
    /// Display name for the participant (minimal bootstrap metadata).
    /// No PII - just a friendly name for API responses.
    /// </summary>
    public string? DisplayName { get; private set; }

    /// <summary>
    /// When this participant was registered.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Whether this participant is active (can authenticate).
    /// </summary>
    public bool IsActive { get; private set; } = true;

    private Participant() { }

    /// <summary>
    /// Creates a new Participant with a unique OIDC-shaped sub claim.
    /// </summary>
    public static Participant Create(string? displayName = null)
    {
        var id = Guid.NewGuid();
        return new Participant
        {
            Id = id,
            Sub = $"participant:{id}",
            DisplayName = displayName,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };
    }

    /// <summary>
    /// Deactivates this participant, preventing authentication.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}
