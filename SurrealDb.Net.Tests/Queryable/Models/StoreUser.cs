using System.ComponentModel.DataAnnotations.Schema;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Tests.Queryable.Models;

[Table("user")]
public class StoreUser : SurrealDbRecord
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [CborIgnoreIfDefault]
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
