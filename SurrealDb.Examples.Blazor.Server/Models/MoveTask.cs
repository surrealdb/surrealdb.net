using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class MoveTask : Record
{
    internal const string Table = "move_task";

    public Thing Task { get; set; } = null!;
    public Thing From { get; set; } = null!;
    public Thing To { get; set; } = null!;
    public int NewIndex { get; set; }
}
