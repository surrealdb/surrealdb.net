using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class MoveTask : Record
{
    internal const string Table = "move_task";

    public RecordId Task { get; set; } = null!;
    public RecordId From { get; set; } = null!;
    public RecordId To { get; set; } = null!;
    public int NewIndex { get; set; }
}
