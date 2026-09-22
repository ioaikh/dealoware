namespace Dealoware.Application.Profile.Dtos;

/// <summary>
/// Response DTO for authenticated self-profile.
/// Fields are projected based on IFieldPolicy - denied fields are omitted (not null).
/// </summary>
public class ProfileResponse
{
    /// <summary>
    /// The participant's unique subject identifier (always included).
    /// </summary>
    public string Sub { get; set; } = string.Empty;

    /// <summary>
    /// Display name (FieldClass: DisplayName).
    /// Soft/illustrative - included for User and OwnAgent.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Login email (FieldClass: LoginEmail).
    /// Only included for User principal; OwnAgent denied.
    /// Omitted entirely when denied (not returned as null).
    /// </summary>
    public string? LoginEmail { get; set; }

    /// <summary>
    /// Contact email (FieldClass: ContactEmail).
    /// Included for User (R/W) and OwnAgent (Read only).
    /// Omitted entirely when denied (not returned as null).
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Whether LoginEmail field is included in this response.
    /// False indicates the field was omitted due to policy denial.
    /// </summary>
    public bool IncludesLoginEmail { get; set; }

    /// <summary>
    /// Whether ContactEmail field is included in this response.
    /// False indicates the field was omitted due to policy denial.
    /// </summary>
    public bool IncludesContactEmail { get; set; }

    /// <summary>
    /// When the participant was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
