using System.Net;
using System.Net.Http.Json;
using TodoApi.Todos;

namespace TodoApi.Tests;

public class OwnershipTests(TodoApiFactory factory)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task List_holds_only_the_users_own_todos()
    {
        var owner = await factory.CreateUserClientAsync();
        var other = await factory.CreateUserClientAsync();
        var ownTodo = await owner.CreateTodoAsync("Water the plants");
        await other.CreateTodoAsync("Feed the cat");

        var todos = await owner.GetFromJsonAsync<TodoResponse[]>("/api/todos", CancellationToken);

        Assert.Equal(new[] { ownTodo }, todos);
    }

    [Fact]
    public async Task Another_users_todo_is_not_found()
    {
        var (_, other, todo) = await TodoOfAnotherUserAsync();

        var response = await other.GetAsync($"/api/todos/{todo.Id}", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Another_users_todo_cannot_be_updated()
    {
        var (owner, other, todo) = await TodoOfAnotherUserAsync();

        var response = await other.PutAsJsonAsync($"/api/todos/{todo.Id}", new TodoRequest("Hijacked"), CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(todo, await owner.GetFromJsonAsync<TodoResponse>($"/api/todos/{todo.Id}", CancellationToken));
    }

    [Fact]
    public async Task Another_users_todo_cannot_be_deleted()
    {
        var (owner, other, todo) = await TodoOfAnotherUserAsync();

        var response = await other.DeleteAsync($"/api/todos/{todo.Id}", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(todo, await owner.GetFromJsonAsync<TodoResponse>($"/api/todos/{todo.Id}", CancellationToken));
    }

    private async Task<(HttpClient Owner, HttpClient Other, TodoResponse Todo)> TodoOfAnotherUserAsync()
    {
        var owner = await factory.CreateUserClientAsync();
        var other = await factory.CreateUserClientAsync();
        return (owner, other, await owner.CreateTodoAsync("Water the plants"));
    }
}
