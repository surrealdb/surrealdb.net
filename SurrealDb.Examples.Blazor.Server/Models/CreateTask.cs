using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class CreateTask : Record
{
    internal const string Table = "create_task";

    public string Title { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public RecordId Column { get; set; } = null!;
}
