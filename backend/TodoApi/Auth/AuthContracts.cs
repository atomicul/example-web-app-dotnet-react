using System.Text.Json.Serialization;

namespace TodoApi.Auth;

/// <summary>An email change the signed-in user asked for and hasn't confirmed yet.</summary>
/// <param name="Email">
/// The new email, which the link sent to it confirms; null when no change is waiting, or its link no longer works
/// because it expired, the change was confirmed or the password changed since.
/// </param>
public record PendingEmailResponse(string? Email);

/// <summary>How a user finishes a login that asks for a second factor.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TwoFactorMethod>))]
public enum TwoFactorMethod
{
    /// <summary>With a code emailed to them at each login.</summary>
    Email,

    /// <summary>With a code their authenticator app shows, once they set it up with <c>POST /manage/2fa</c>.</summary>
    Authenticator,
}

/// <summary>How the signed-in user finishes a login.</summary>
/// <param name="IsAvailable">
/// Whether logins ask for a second factor at all; while they don't, the authenticator app can't be set up.
/// </param>
/// <param name="Method">The way the user chose, the email unless they set up an authenticator app.</param>
public record TwoFactorMethodResponse(bool IsAvailable, TwoFactorMethod Method);
