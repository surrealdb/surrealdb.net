using System.ComponentModel.DataAnnotations.Schema;
using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class TaskRecord : Record
{
    internal const string Table = "task";

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("due_date")]
    public DateTime DueDate { get; set; }

    [Column("created_at")]
    [CborIgnoreIfDefault]
    public DateTime CreatedAt { get; set; }
}
