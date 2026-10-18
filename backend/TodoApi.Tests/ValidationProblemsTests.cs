using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Validation;

namespace TodoApi.Tests;

/// <summary>
/// Runs <see cref="ValidationProblems.ValidateRequests"/> on its own, for contracts no endpoint of the API has yet,
/// such as one with an object nested in it.
/// </summary>
public class ValidationProblemsTests
{
    public record Address([property: MaxLength(5)] string? PostalCode);

    public record Person([property: Required] string? Name, Address? Address);

    private static readonly object Handled = new();

    [Fact]
    public async Task Nested_object_errors_are_about_the_nested_field()
    {
        var problem = Assert.IsType<ValidationProblem>(await ValidateAsync(new Person("Ada", new Address("123456"))));

        Assert.Equal(["Address.PostalCode"], problem.ProblemDetails.Errors.Keys);
        var issue = Assert.Single(IssuesOf(problem));
        Assert.Equal("#/address/postalCode", issue.GetProperty("pointer").GetString());
        Assert.Equal("MaxLength", issue.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Valid_nested_object_lets_the_request_through()
    {
        Assert.Same(Handled, await ValidateAsync(new Person("Ada", new Address("12345"))));
    }

    [Fact]
    public async Task Missing_nested_object_is_not_validated()
    {
        Assert.Same(Handled, await ValidateAsync(new Person("Ada", null)));
    }

    private static void Save(Person person)
    {
    }

    private static async Task<object?> ValidateAsync(Person person)
    {
        var context = new EndpointFilterFactoryContext
        {
            MethodInfo = typeof(ValidationProblemsTests)
                .GetMethod(nameof(Save), BindingFlags.NonPublic | BindingFlags.Static)!,
            ApplicationServices = new ServiceCollection().BuildServiceProvider(),
        };
        var filter = ValidationProblems.ValidateRequests(context, _ => ValueTask.FromResult<object?>(Handled));
        return await filter(new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), person));
    }

    /// <summary>The problem's issues, as clients read them.</summary>
    private static JsonElement[] IssuesOf(ValidationProblem problem)
    {
        var issues = problem.ProblemDetails.Extensions[ValidationProblems.IssuesMember];
        return [.. JsonSerializer.SerializeToElement(issues, JsonSerializerOptions.Web).EnumerateArray()];
    }
}
