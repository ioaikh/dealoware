namespace Dealoware.Application.Profile.Dtos;

/// <summary>
/// Request DTO for updating self-profile.
/// Only fields that are allowed by IFieldPolicy for the principal can be updated.
/// </summary>
public class UpdateProfileRequest
{
    /// <summary>
    /// New display name (optional).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// New login email (optional).
    /// Only User principal can update - OwnAgent denied.
    /// </summary>
    public string? LoginEmail { get; set; }

    /// <summary>
    /// New contact email (optional).
    /// Only User principal can update - OwnAgent Read only.
    /// </summary>
    public string? ContactEmail { get; set; }
}
