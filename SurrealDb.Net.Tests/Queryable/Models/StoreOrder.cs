using System.ComponentModel.DataAnnotations.Schema;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Tests.Queryable.Models;

[Table("orders")]
public class StoreOrder : SurrealDbRecord
{
    [Column("user")]
    public StoreUser User { get; set; } = null!;

    [Column("products")]
    public StoreProduct[] Products { get; set; } = [];

    [Column("total")]
    public float Total { get; set; }

    [CborIgnoreIfDefault]
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
