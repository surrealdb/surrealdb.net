using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Tests.Queryable.Models;

public class Order : SurrealDbRecord
{
    public RecordId[] Products { get; set; } = [];

    [CborIgnoreIfDefault]
    public DateTime? CreatedAt { get; set; }

    [CborIgnoreIfDefault]
    public string? Status { get; set; }

    public Address Address { get; set; } = null!;
}
