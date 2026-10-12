namespace TodoApi.Todos;

public class Todo
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public bool IsComplete { get; set; }
    public DateOnly? DueDate { get; set; }
    public string OwnerId { get; set; } = string.Empty;
}
