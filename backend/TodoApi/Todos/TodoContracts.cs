using System.ComponentModel.DataAnnotations;
using TodoApi.Validation;

namespace TodoApi.Todos;

/// <summary>Payload used to create or replace a todo.</summary>
/// <param name="Title">Short description of the task.</param>
/// <param name="IsComplete">Whether the task is done.</param>
/// <param name="DueDate">Optional date the task is due.</param>
public record TodoRequest([property: Required, StringLength(200, MinimumLength = 1)] string Title, bool IsComplete = false, DateOnly? DueDate = null)
    : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsComplete && DueDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            yield return new CodedValidationResult("DueDateInPast", "An open todo can't be due in the past.", [nameof(DueDate)]);
        }
    }
}

/// <summary>A todo owned by the current user.</summary>
/// <param name="Id">Identifier of the todo.</param>
/// <param name="Title">Short description of the task.</param>
/// <param name="IsComplete">Whether the task is done.</param>
/// <param name="DueDate">Optional date the task is due.</param>
public record TodoResponse(int Id, string Title, bool IsComplete, DateOnly? DueDate)
{
    public static TodoResponse From(Todo todo) => new(todo.Id, todo.Title, todo.IsComplete, todo.DueDate);
}
