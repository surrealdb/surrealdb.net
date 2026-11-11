using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Attributes;

namespace SurrealDb.Net.Tests.Queryable.Models;

[Table("friends")]
public class Friends : SurrealDbRelationRecord
{
    [SurrealIn]
    [JsonIgnore]
    [CborIgnore]
    public StoreUser FirstUser { get; } = default!;

    [SurrealOut]
    [JsonIgnore]
    [CborIgnore]
    public StoreUser SecondUser { get; } = default!;
}
