using System.ComponentModel.DataAnnotations.Schema;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Tests.Queryable.Models;

[Table("warehouse")]
public class Warehouse : SurrealDbRecord
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("location")]
    public string Location { get; set; } = string.Empty;

    [CborIgnoreIfDefault]
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
