using System.Text.Json.Serialization;
using Dealoware.Domain.Admin;
using Dealoware.Infrastructure.Admin;

namespace Dealoware.Api.Admin;

/// <summary>
/// The server actions of S-A12 security settings (T04), as JSON under
/// <c>/admin/api/settings/security</c>. Every route is behind the signed-in
/// CoreOwner session; every POST is behind the Origin gate and the keyed
/// anti-forgery token of <see cref="AdminSessionMiddleware"/>. Nothing here is
/// on an exemption list. The page is a later piece.
/// <para>
/// Password change: the current password plus a current authenticator or
/// recovery code, checked together on that submit (S-A12 note v1.4).
/// New recovery codes and authenticator re-enrolment: a step-up within the
/// last five minutes (TD-ADM-174), given on <see cref="StepUpPath"/> or sent
/// with the action itself.
/// </para>
/// </summary>
public static class AdminSecuritySettingsEndpoints
{
    public const string BasePath = "/admin/api/settings/security";
    public const string ActivityPath = BasePath + "/activity";
    public const string PasswordPath = BasePath + "/password";
    public const string StepUpPath = BasePath + "/step-up";
    public const string RecoveryCodesPath = BasePath + "/recovery-codes";
    public const string AuthenticatorPath = BasePath + "/authenticator";
    public const string AuthenticatorConfirmPath = AuthenticatorPath + "/confirm";

    public static IServiceCollection AddAdminSecuritySettings(this IServiceCollection services)
    {
        services.AddScoped<AdminSecuritySettingsStore>();
        services.AddScoped<AdminSecuritySettingsService>();
        return services;
    }

    public static void MapAdminSecuritySettings(this WebApplication app)
    {
        app.MapGet(BasePath, (HttpContext http, AdminSecuritySettingsService service) => service.GetStateAsync(http))
            .WithName("AdminSecuritySettingsState")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapGet(ActivityPath, (HttpContext http, AdminSecuritySettingsService service) => service.GetActivityAsync(http))
            .WithName("AdminSecuritySettingsActivity")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapPost(PasswordPath, (PasswordChangeBody? body, HttpContext http, AdminSecuritySettingsService service)
                => service.ChangePasswordAsync(body, http))
            .WithName("AdminSecuritySettingsPassword")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapPost(StepUpPath, (StepUpBody? body, HttpContext http, AdminSecuritySettingsService service)
                => service.StepUpAsync(body, http))
            .WithName("AdminSecuritySettingsStepUp")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapPost(RecoveryCodesPath, (StepUpBody? body, HttpContext http, AdminSecuritySettingsService service)
                => service.RegenerateRecoveryCodesAsync(body, http))
            .WithName("AdminSecuritySettingsRecoveryCodes")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        app.MapPost(AuthenticatorPath, (StepUpBody? body, HttpContext http, AdminSecuritySettingsService service)
                => service.StartReenrolmentAsync(body, http))
            .WithName("AdminSecuritySettingsAuthenticatorStart")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        app.MapPost(AuthenticatorConfirmPath, (ReenrolConfirmBody? body, HttpContext http, AdminSecuritySettingsService service)
                => service.ConfirmReenrolmentAsync(body, http))
            .WithName("AdminSecuritySettingsAuthenticatorConfirm")
            .WithTags("Admin")
            .RequireAuthorization(AdminSessionMiddleware.PolicyName)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    /// <summary>The step-up: the current password plus exactly one of a current authenticator code or a recovery code.</summary>
    public class StepUpBody
    {
        [JsonPropertyName("currentPassword")]
        public string? CurrentPassword { get; set; }

        [JsonPropertyName("totpCode")]
        public string? TotpCode { get; set; }

        [JsonPropertyName("recoveryCode")]
        public string? RecoveryCode { get; set; }
    }

    /// <summary>S-A12 Change password. Confirm new password is checked by the page and never sent.</summary>
    public sealed class PasswordChangeBody : StepUpBody
    {
        [JsonPropertyName("newPassword")]
        public string? NewPassword { get; set; }
    }

    /// <summary>A code from the new key, to confirm a re-enrolment.</summary>
    public sealed class ReenrolConfirmBody
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }
}
