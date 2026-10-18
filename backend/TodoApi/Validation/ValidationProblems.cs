using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace TodoApi.Validation;

/// <summary>
/// Builds the API's validation problems: ASP.NET's own <see cref="HttpValidationProblemDetails"/>, whose <c>errors</c>
/// map reads as the framework writes it, plus an <c>issues</c> member that lists each error as an RFC 9457 error
/// object with a code clients can word it by.
/// </summary>
public static class ValidationProblems
{
    public const string IssuesMember = "issues";

    public static ValidationProblem For(IEnumerable<ValidationError> errors)
    {
        var list = errors.ToList();
        var map = list
            .GroupBy(error => error.Member ?? error.Code)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Detail).ToArray());
        var issues = list.Select(ValidationIssue.From).ToList();
        return TypedResults.ValidationProblem(map, extensions: new Dictionary<string, object?> { [IssuesMember] = issues });
    }

    /// <summary>
    /// Endpoint filter factory that validates every argument whose type has validation attributes or implements
    /// <see cref="IValidatableObject"/>, and answers with a validation problem when one fails. It validates the
    /// argument's own properties only, not objects or collections nested in them.
    /// </summary>
    public static EndpointFilterDelegate ValidateRequests(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        var positions = context.MethodInfo.GetParameters()
            .Where(parameter => HasRules(parameter.ParameterType))
            .Select(parameter => parameter.Position)
            .ToList();
        if (positions.Count == 0)
        {
            return next;
        }

        return async invocation =>
        {
            var errors = positions
                .Select(position => invocation.Arguments[position])
                .OfType<object>()
                .SelectMany(Validate)
                .ToList();
            return errors.Count > 0 ? For(errors) : await next(invocation);
        };
    }

    /// <summary>
    /// Endpoint filter for Identity's endpoints, which key each validation error by its code: answers their validation
    /// problems with the same errors, coded by that key.
    /// </summary>
    public static async ValueTask<object?> CodeIdentityErrors(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);
        var problem = (result as INestedHttpResult)?.Result as ValidationProblem ?? result as ValidationProblem;
        return problem is null
            ? result
            : For(problem.ProblemDetails.Errors.SelectMany(error =>
                error.Value.Select(detail => new ValidationError(null, error.Key, detail))));
    }

    private static bool HasRules(Type type) =>
        type.IsAssignableTo(typeof(IValidatableObject))
        || type.GetProperties().Any(property => property.IsDefined(typeof(ValidationAttribute), inherit: true));

    /// <summary>The errors of the request's properties or, when they are all valid, of its own rules.</summary>
    private static IEnumerable<ValidationError> Validate(object request)
    {
        var propertyErrors = request.GetType().GetProperties()
            .SelectMany(property => PropertyErrors(request, property))
            .ToList();
        return propertyErrors.Count > 0 || request is not IValidatableObject validatable
            ? propertyErrors
            : validatable.Validate(new ValidationContext(request)).SelectMany(RuleErrors);
    }

    /// <summary>
    /// The errors of a property's validation attributes, each coded by its attribute's name. A missing required value
    /// is its only error, as <see cref="Validator"/> reports it.
    /// </summary>
    private static IEnumerable<ValidationError> PropertyErrors(object request, PropertyInfo property)
    {
        var value = property.GetValue(request);
        var attributes = property.GetCustomAttributes<ValidationAttribute>(inherit: true).ToList();
        var missing = attributes.OfType<RequiredAttribute>().Where(required => !required.IsValid(value)).ToList<ValidationAttribute>();
        var context = new ValidationContext(request) { MemberName = property.Name, DisplayName = property.Name };

        return (missing.Count > 0 ? missing : attributes)
            .Select(attribute => (Code: CodeOf(attribute), Result: attribute.GetValidationResult(value, context)))
            .Where(check => check.Result != ValidationResult.Success)
            .Select(check => new ValidationError(property.Name, check.Code, check.Result!.ErrorMessage ?? check.Code));
    }

    /// <summary>The errors of a failed rule, one per member it names, coded as it declares.</summary>
    private static IEnumerable<ValidationError> RuleErrors(ValidationResult result)
    {
        var code = (result as CodedValidationResult)?.Code ?? nameof(IValidatableObject.Validate);
        var detail = result.ErrorMessage ?? code;
        return result.MemberNames.Any()
            ? result.MemberNames.Select(member => new ValidationError(member, code, detail))
            : [new ValidationError(null, code, detail)];
    }

    private static string CodeOf(ValidationAttribute attribute)
    {
        var name = attribute.GetType().Name;
        return name.EndsWith(nameof(Attribute)) ? name[..^nameof(Attribute).Length] : name;
    }

    /// <summary>An error as RFC 9457 lists it: a JSON Pointer to the request field it is about, what is wrong and its code.</summary>
    private sealed record ValidationIssue(
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Pointer,
        string Detail,
        string Code)
    {
        public static ValidationIssue From(ValidationError error) =>
            new(error.Member is null ? null : $"#/{JsonNamingPolicy.CamelCase.ConvertName(error.Member)}", error.Detail, error.Code);
    }
}
