namespace TodoApi.Validation;

/// <summary>A reason the API refuses a request.</summary>
/// <param name="Member">The request property the error is about, or null when it is not about one.</param>
/// <param name="Code">Names the error, for clients to word it in their own language.</param>
/// <param name="Detail">What is wrong, in English.</param>
public record ValidationError(string? Member, string Code, string Detail);
