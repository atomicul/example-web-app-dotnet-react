using TodoApi.Data;

namespace TodoApi.Todos;

public class Todo : IOwnedByUser
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public bool IsComplete { get; set; }
    public DateOnly? DueDate { get; set; }
    public string OwnerId { get; set; } = string.Empty;
}
