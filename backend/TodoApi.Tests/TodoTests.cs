using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using TodoApi.Todos;

namespace TodoApi.Tests;

public class TodoTests(TodoApiFactory factory)
{
    private static readonly DateOnly Yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Create_returns_the_todo_at_its_location()
    {
        var client = await factory.CreateUserClientAsync();

        var created = await client.PostAsJsonAsync("/api/todos", new TodoRequest("Water the plants"), CancellationToken);
        var todo = await created.Content.ReadFromJsonAsync<TodoResponse>(CancellationToken);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(todo, await client.GetFromJsonAsync<TodoResponse>(created.Headers.Location, CancellationToken));
    }

    [Fact]
    public async Task Update_replaces_the_todo()
    {
        var client = await factory.CreateUserClientAsync();
        var todo = await client.CreateTodoAsync("Water the plants");

        var updated = await client.PutAsJsonAsync($"/api/todos/{todo.Id}", new TodoRequest("Water the cactus", true, Yesterday), CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        Assert.Equal(
            new TodoResponse(todo.Id, "Water the cactus", true, Yesterday),
            await client.GetFromJsonAsync<TodoResponse>($"/api/todos/{todo.Id}", CancellationToken));
    }

    [Fact]
    public async Task Delete_removes_the_todo()
    {
        var client = await factory.CreateUserClientAsync();
        var todo = await client.CreateTodoAsync("Water the plants");

        var deleted = await client.DeleteAsync($"/api/todos/{todo.Id}", CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/todos/{todo.Id}", CancellationToken)).StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task Title_must_have_1_to_200_characters(int length)
    {
        var client = await factory.CreateUserClientAsync();

        var created = await client.PostAsJsonAsync("/api/todos", new TodoRequest(new string('a', length)), CancellationToken);

        Assert.Contains("Title", await ValidationErrorsAsync(created));
    }

    [Fact]
    public async Task Open_todo_cannot_be_due_in_the_past()
    {
        var client = await factory.CreateUserClientAsync();

        var created = await client.PostAsJsonAsync("/api/todos", new TodoRequest("Water the plants", DueDate: Yesterday), CancellationToken);

        Assert.Contains("DueDate", await ValidationErrorsAsync(created));
    }

    [Fact]
    public async Task Error_the_api_words_carries_its_code()
    {
        var client = await factory.CreateUserClientAsync();

        var created = await client.PostAsJsonAsync("/api/todos", new TodoRequest("Water the plants", DueDate: Yesterday), CancellationToken);

        Assert.Equal(["DueDateInPast"], (await ValidationCodesAsync(created))["DueDate"]);
    }

    [Fact]
    public async Task Error_worded_by_an_attribute_has_no_code()
    {
        var client = await factory.CreateUserClientAsync();

        var created = await client.PostAsJsonAsync("/api/todos", new TodoRequest(""), CancellationToken);

        Assert.Equal([null], (await ValidationCodesAsync(created))["Title"]);
    }

    [Fact]
    public async Task Completed_todo_can_be_due_in_the_past()
    {
        var client = await factory.CreateUserClientAsync();

        var created = await client.PostAsJsonAsync("/api/todos", new TodoRequest("Water the plants", true, Yesterday), CancellationToken);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    private static async Task<IEnumerable<string>> ValidationErrorsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(CancellationToken);
        return problem!.Errors.Keys;
    }

    private static async Task<Dictionary<string, string?[]>> ValidationCodesAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<CodedValidationProblem>(CancellationToken);
        return problem!.Codes;
    }

    private record CodedValidationProblem(Dictionary<string, string?[]> Codes);
}
