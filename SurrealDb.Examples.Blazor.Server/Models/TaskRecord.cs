using System.Text.Json.Serialization;
using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class TaskRecord : Record
{
    internal const string Table = "task";

    public string Title { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime CreatedAt { get; set; }
}
