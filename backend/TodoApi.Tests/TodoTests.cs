using System.Net;
using System.Net.Http.Json;
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
    [InlineData(0, "Required")]
    [InlineData(201, "StringLength")]
    public async Task Title_must_have_1_to_200_characters(int length, string code)
    {
        var client = await factory.CreateUserClientAsync();

        var created = await client.PostAsJsonAsync("/api/todos", new TodoRequest(new string('a', length)), CancellationToken);
        var (problem, issues) = await created.ReadValidationProblemAsync();

        Assert.Equal(problem.Errors["Title"], issues.Select(issue => issue.Detail));
        Assert.Equal(new ValidationProblemReader.Issue("#/title", problem.Errors["Title"][0], code), Assert.Single(issues));
    }

    [Fact]
    public async Task Open_todo_cannot_be_due_in_the_past()
    {
        var client = await factory.CreateUserClientAsync();

        var created = await client.PostAsJsonAsync("/api/todos", new TodoRequest("Water the plants", DueDate: Yesterday), CancellationToken);
        var (problem, issues) = await created.ReadValidationProblemAsync();

        const string Detail = "An open todo can't be due in the past.";
        Assert.Equal([Detail], problem.Errors["DueDate"]);
        Assert.Equal(new ValidationProblemReader.Issue("#/dueDate", Detail, "DueDateInPast"), Assert.Single(issues));
    }

    [Fact]
    public async Task Completed_todo_can_be_due_in_the_past()
    {
        var client = await factory.CreateUserClientAsync();

        var created = await client.PostAsJsonAsync("/api/todos", new TodoRequest("Water the plants", true, Yesterday), CancellationToken);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }
}
