using TodoApi.Todos;

namespace TodoApi;

/// <summary>
/// Adds a <c>codes</c> member to every validation problem, naming each error the API words itself, so clients can word
/// it in their own language. It has the shape of <c>errors</c>: each code names the error at the same position, and is
/// null for an error the API has no code for, such as one worded by a validation attribute or by Identity.
/// </summary>
public static class ValidationCodes
{
    public const string Member = "codes";

    private static readonly Dictionary<string, string> CodesByMessage = new()
    {
        [TodoRequest.DueDateInPast] = nameof(TodoRequest.DueDateInPast),
    };

    public static void Add(ProblemDetailsContext context)
    {
        if (context.ProblemDetails is HttpValidationProblemDetails problem)
        {
            problem.Extensions[Member] = problem.Errors.ToDictionary(
                error => error.Key,
                error => error.Value.Select(message => CodesByMessage.GetValueOrDefault(message)).ToArray());
        }
    }
}
