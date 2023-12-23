using SurrealDb.Net.Models;
using System.Text.Json.Serialization;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class ColumnRecord : Record
{
    internal const string Table = "column";

    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public IEnumerable<Thing> Tasks { get; set; } = Array.Empty<Thing>();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime CreatedAt { get; set; }
}
