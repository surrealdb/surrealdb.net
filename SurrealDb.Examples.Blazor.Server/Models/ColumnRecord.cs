using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class ColumnRecord : Record
{
    internal const string Table = "column";

    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public IEnumerable<Thing> Tasks { get; set; } = Array.Empty<Thing>();

    [CborIgnoreIfDefault]
    public DateTime CreatedAt { get; set; }
}
