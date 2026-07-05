using System.ComponentModel.DataAnnotations.Schema;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Tests.Queryable.Models;

[Table("purchased")]
public class Purchased : SurrealDbRelationRecord
{
    [Column("quantity")]
    public int Quantity { get; set; }

    [CborIgnoreIfDefault]
    [Column("order")]
    public StoreOrder? Order { get; set; }

    [CborIgnoreIfDefault]
    [Column("purchased_at")]
    public DateTime? PurchasedAt { get; set; }
}
