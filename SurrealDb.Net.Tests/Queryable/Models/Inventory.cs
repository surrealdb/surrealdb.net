using System.ComponentModel.DataAnnotations.Schema;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Tests.Queryable.Models;

[Table("inventory")]
public class Inventory : SurrealDbRecord
{
    public RecordId Product { get; set; } = null!;

    public RecordId Warehouse { get; set; } = null!;

    public int Quantity { get; set; }

    [CborIgnoreIfDefault]
    public DateTime? CreatedAt { get; set; }
}
