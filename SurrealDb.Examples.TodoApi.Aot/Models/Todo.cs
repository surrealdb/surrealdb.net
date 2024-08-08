using System.Text.Json.Serialization;
using SurrealDb.Net.Models;

namespace SurrealDb.Examples.TodoApi.Aot.Models;

public class Todo : Record
{
    public string? Title { get; set; }
    public DateOnly? DueBy { get; set; } = null;
    public bool IsComplete { get; set; } = false;

    [JsonConstructor]
    public Todo() { }

    public Todo(int id, string title, DateOnly? dueBy = null, bool isComplete = false)
    {
        Id = RecordId.From(Table, id);
        Title = title;
        DueBy = dueBy;
        IsComplete = isComplete;
    }

    internal const string Table = "todo";
}
