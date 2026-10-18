using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace TodoApi.Auth;

/// <summary>
/// Describes the rules Identity applies to a new email and password in the schemas of the requests that set them,
/// <see cref="RegisterRequest"/> and <see cref="InfoRequest"/>, so clients can check them before sending: the
/// characters a user name (the email) may use, and the password requirements.
/// <see cref="PasswordOptions.RequiredUniqueChars"/> can't be written as a pattern and is left to the server.
/// </summary>
public class CredentialRulesSchemaTransformer(IOptions<IdentityOptions> identityOptions) : IOpenApiSchemaTransformer
{
    /// <summary>The email and password properties of each request that sets them.</summary>
    private static readonly Dictionary<Type, (string Email, string Password)> CredentialProperties = new()
    {
        [typeof(RegisterRequest)] = ("email", "password"),
        [typeof(InfoRequest)] = ("newEmail", "newPassword"),
    };

    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (!CredentialProperties.TryGetValue(context.JsonTypeInfo.Type, out var names)
            || schema.Properties?[names.Email] is not OpenApiSchema email
            || schema.Properties[names.Password] is not OpenApiSchema password)
        {
            return Task.CompletedTask;
        }

        var allowedCharacters = identityOptions.Value.User.AllowedUserNameCharacters;
        email.Pattern = EmailPattern(allowedCharacters);
        email.Description = EmailDescription(allowedCharacters);

        var passwordOptions = identityOptions.Value.Password;
        password.MinLength = passwordOptions.RequiredLength;
        password.Pattern = PasswordPattern(passwordOptions);
        password.Description = PasswordDescription(passwordOptions);
        return Task.CompletedTask;
    }

    /// <summary>One <c>@</c> between two non-empty parts, as the email check requires, using only allowed characters.</summary>
    private static string EmailPattern(string allowedCharacters)
    {
        var part = string.IsNullOrEmpty(allowedCharacters)
            ? @"[^@\r\n]+"
            : $"[{string.Concat(allowedCharacters.Where(character => character != '@').Select(EscapeInClass))}]+";
        return $"^{part}@{part}$";
    }

    private static string EmailDescription(string allowedCharacters)
    {
        var symbols = allowedCharacters.Where(character => character != '@' && !char.IsAsciiLetterOrDigit(character));
        return string.IsNullOrEmpty(allowedCharacters)
            ? "An email address."
            : $"An email address made of letters, digits and {string.Concat(symbols)}";
    }

    private static string? PasswordPattern(PasswordOptions options)
    {
        var lookaheads = PasswordRules(options).Select(rule => $"(?=.*{rule.Pattern})").ToList();
        return lookaheads.Count > 0 ? $"^{string.Concat(lookaheads)}" : null;
    }

    private static string PasswordDescription(PasswordOptions options)
    {
        var kinds = PasswordRules(options).Select(rule => rule.Name).ToList();
        var length = $"Use at least {options.RequiredLength} characters";
        return kinds.Count switch
        {
            0 => $"{length}.",
            1 => $"{length}, with {kinds[0]}.",
            _ => $"{length}, with {string.Join(", ", kinds[..^1])} and {kinds[^1]}.",
        };
    }

    /// <summary>The kinds of character a password must contain, matching Identity's ASCII-only checks.</summary>
    private static IEnumerable<(string Name, string Pattern)> PasswordRules(PasswordOptions options)
    {
        if (options.RequireUppercase) yield return ("an uppercase letter", "[A-Z]");
        if (options.RequireLowercase) yield return ("a lowercase letter", "[a-z]");
        if (options.RequireDigit) yield return ("a digit", "[0-9]");
        if (options.RequireNonAlphanumeric) yield return ("a symbol", "[^a-zA-Z0-9]");
    }

    private static string EscapeInClass(char character) =>
        @"\]^-[".Contains(character) ? $@"\{character}" : character.ToString();
}
