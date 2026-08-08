using System.ComponentModel.DataAnnotations.Schema;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Tests.Queryable.Models;

[Table("inventory")]
public class Inventory : SurrealDbRecord
{
    [Column("product")]
    public StoreProduct Product { get; set; } = null!;

    [Column("warehouse")]
    public Warehouse Warehouse { get; set; } = null!;

    [Column("quantity")]
    public int Quantity { get; set; }

    [CborIgnoreIfDefault]
    public DateTime? CreatedAt { get; set; }
}
