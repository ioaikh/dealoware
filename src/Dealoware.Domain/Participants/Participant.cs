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
    /// FieldClass: DisplayName - soft/illustrative.
    /// </summary>
    public string? DisplayName { get; private set; }

    /// <summary>
    /// Login credential email (highly sensitive).
    /// FieldClass: LoginEmail - User R/W; OwnAgent Deny all; counterparty/stranger Deny.
    /// </summary>
    public string? LoginEmail { get; private set; }

    /// <summary>
    /// Contact email for business communication.
    /// FieldClass: ContactEmail - User R/W; OwnAgent Read; counterparty Deny; ShareOutbound Deny.
    /// </summary>
    public string? ContactEmail { get; private set; }

    /// <summary>
    /// When this participant was registered.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Last mutation time (UTC). Null until a later write sets it.
    /// Sort by updated falls back to CreatedAt when this is null.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    /// Whether this participant is active (can authenticate).
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Soft-delete marker. When non-null, indicates the entity is soft-deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>
    /// Concurrency token for optimistic concurrency control.
    /// </summary>
    public uint Version { get; private set; }

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

    /// <summary>
    /// Updates the login email (owner-only operation).
    /// </summary>
    public void UpdateLoginEmail(string? loginEmail)
    {
        LoginEmail = loginEmail;
    }

    /// <summary>
    /// Updates the contact email (owner-only operation).
    /// </summary>
    public void UpdateContactEmail(string? contactEmail)
    {
        ContactEmail = contactEmail;
    }

    /// <summary>
    /// Updates the display name.
    /// </summary>
    public void UpdateDisplayName(string? displayName)
    {
        DisplayName = displayName;
    }

    /// <summary>
    /// Soft-deletes this participant. Sets DeletedAt and bumps Version.
    /// Already-deleted rows are left unchanged.
    /// </summary>
    public void SoftDelete(DateTimeOffset deletedAt)
    {
        if (DeletedAt is not null)
            return;

        DeletedAt = deletedAt;
        UpdatedAt = deletedAt;
        Version++;
    }
}
