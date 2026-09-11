namespace Dealoware.Application.Auth.Dtos;

/// <summary>
/// Request to register/bootstrap a new participant.
/// Minimal bootstrap metadata only - no PII.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Optional display name for the participant.
    /// </summary>
    public string? DisplayName { get; set; }
}
