using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace TodoApi.Tests;

/// <summary>Reads the validation problems the API refuses requests with.</summary>
public static class ValidationProblemReader
{
    public record Issue(string? Pointer, string Detail, string Code);

    private record WithIssues(Issue[] Issues);

    /// <summary>Asserts a 400 and reads its problem, as clients of ASP.NET's shape do, and its issues.</summary>
    public static async Task<(HttpValidationProblemDetails Problem, Issue[] Issues)> ReadValidationProblemAsync(
        this HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var problem = JsonSerializer.Deserialize<HttpValidationProblemDetails>(body, JsonSerializerOptions.Web);
        var withIssues = JsonSerializer.Deserialize<WithIssues>(body, JsonSerializerOptions.Web);
        return (problem!, withIssues!.Issues);
    }
}
