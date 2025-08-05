using System.ComponentModel.DataAnnotations.Schema;
using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class ColumnRecord : Record
{
    internal const string Table = "column";

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("order")]
    public int Order { get; set; }

    [Column("tasks")]
    public IEnumerable<RecordId> Tasks { get; set; } = [];

    [Column("created_at")]
    [CborIgnoreIfDefault]
    public DateTime CreatedAt { get; set; }
}
