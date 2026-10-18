using System.ComponentModel.DataAnnotations;

namespace TodoApi.Validation;

/// <summary>
/// A failed rule of <see cref="IValidatableObject.Validate"/> with the code clients word it by, sent next to its
/// message in the validation problem's <c>issues</c>.
/// </summary>
public class CodedValidationResult(string code, string errorMessage, IEnumerable<string> memberNames)
    : ValidationResult(errorMessage, memberNames)
{
    public string Code { get; } = code;
}
