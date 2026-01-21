using System.ComponentModel.DataAnnotations.Schema;
using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class DisplayableTask
{
    [Column("id")]
    public RecordId Id { get; set; } = null!;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("due_date")]
    public DateTime DueDate { get; set; }
}
