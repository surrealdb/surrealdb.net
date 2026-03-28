using System.ComponentModel.DataAnnotations.Schema;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Tests.Queryable.Models;

[Table("warehouse")]
public class Warehouse : SurrealDbRecord
{
    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    [CborIgnoreIfDefault]
    public DateTime? CreatedAt { get; set; }
}
