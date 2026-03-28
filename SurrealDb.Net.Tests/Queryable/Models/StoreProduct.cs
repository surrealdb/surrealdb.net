using System.ComponentModel.DataAnnotations.Schema;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Tests.Queryable.Models;

[Table("product")]
public class StoreProduct : SurrealDbRecord
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("price")]
    public float Price { get; set; }

    [CborIgnoreIfDefault]
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
