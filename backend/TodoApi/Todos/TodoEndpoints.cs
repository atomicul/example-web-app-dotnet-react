using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;

namespace TodoApi.Todos;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodos(this IEndpointRouteBuilder app)
    {
        var todos = app.MapGroup("/todos").RequireAuthorization().WithTags("Todos");

        todos.MapGet("/", GetAll).WithName("GetTodos");
        todos.MapGet("/{id:int}", GetById).WithName("GetTodo");
        todos.MapPost("/", Create).WithName("CreateTodo").ProducesValidationProblem();
        todos.MapPut("/{id:int}", Update).WithName("UpdateTodo").ProducesValidationProblem();
        todos.MapDelete("/{id:int}", Delete).WithName("DeleteTodo");

        return app;
    }

    /// <summary>Lists the current user's todos.</summary>
    public static async Task<Ok<TodoResponse[]>> GetAll(AppDbContext db, CancellationToken ct)
    {
        var todos = await db.Todos.AsNoTracking().Select(t => TodoResponse.From(t)).ToArrayAsync(ct);
        return TypedResults.Ok(todos);
    }

    /// <summary>Gets one of the current user's todos.</summary>
    /// <param name="id">Identifier of the todo.</param>
    public static async Task<Results<Ok<TodoResponse>, NotFound>> GetById(int id, AppDbContext db, CancellationToken ct)
    {
        var todo = await db.Todos.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        return todo is null ? TypedResults.NotFound() : TypedResults.Ok(TodoResponse.From(todo));
    }

    /// <summary>Creates a todo owned by the current user.</summary>
    public static async Task<CreatedAtRoute<TodoResponse>> Create(TodoRequest request, AppDbContext db, CancellationToken ct)
    {
        var todo = new Todo { Title = request.Title, IsComplete = request.IsComplete, DueDate = request.DueDate };
        db.Todos.Add(todo);
        await db.SaveChangesAsync(ct);
        return TypedResults.CreatedAtRoute(TodoResponse.From(todo), "GetTodo", new { id = todo.Id });
    }

    /// <summary>Replaces one of the current user's todos.</summary>
    /// <param name="id">Identifier of the todo.</param>
    public static async Task<Results<NoContent, NotFound>> Update(int id, TodoRequest request, AppDbContext db, CancellationToken ct)
    {
        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (todo is null)
        {
            return TypedResults.NotFound();
        }

        todo.Title = request.Title;
        todo.IsComplete = request.IsComplete;
        todo.DueDate = request.DueDate;
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    /// <summary>Deletes one of the current user's todos.</summary>
    /// <param name="id">Identifier of the todo.</param>
    public static async Task<Results<NoContent, NotFound>> Delete(int id, AppDbContext db, CancellationToken ct)
    {
        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (todo is null)
        {
            return TypedResults.NotFound();
        }

        db.Todos.Remove(todo);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
