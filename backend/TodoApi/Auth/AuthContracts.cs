namespace TodoApi.Auth;

/// <summary>An email change the signed-in user asked for and hasn't confirmed yet.</summary>
/// <param name="Email">
/// The new email, which the link sent to it confirms; null when no change is waiting, or its link no longer works
/// because it expired, the change was confirmed or the password changed since.
/// </param>
public record PendingEmailResponse(string? Email);
