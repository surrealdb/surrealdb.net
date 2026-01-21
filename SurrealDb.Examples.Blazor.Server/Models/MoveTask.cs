using System.ComponentModel.DataAnnotations.Schema;
using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class MoveTask : Record
{
    internal const string Table = "move_task";

    [Column("task")]
    public RecordId Task { get; set; } = null!;

    [Column("from")]
    public RecordId From { get; set; } = null!;

    [Column("to")]
    public RecordId To { get; set; } = null!;

    [Column("new_index")]
    public int NewIndex { get; set; }
}
