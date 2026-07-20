using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Attributes;

namespace SurrealDb.Net.Tests.Queryable.Models;

[Table("purchased")]
public class Purchased : SurrealDbRelationRecord
{
    [SurrealIn]
    [JsonIgnore]
    [CborIgnore]
    public StoreUser User { get; } = default!;

    [SurrealOut]
    [JsonIgnore]
    [CborIgnore]
    public StoreProduct Product { get; } = default!;

    [Column("quantity")]
    public int Quantity { get; set; }

    [CborIgnoreIfDefault]
    [Column("order")]
    public StoreOrder? Order { get; set; }

    [CborIgnoreIfDefault]
    [Column("purchased_at")]
    public DateTime? PurchasedAt { get; set; }
}
