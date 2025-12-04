using System.ComponentModel.DataAnnotations.Schema;
using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class CreateTask : Record
{
    internal const string Table = "create_task";

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("due_date")]
    public DateTime DueDate { get; set; }

    [Column("column")]
    public RecordId Column { get; set; } = null!;
}
