using System.ComponentModel.DataAnnotations.Schema;
using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Benchmarks.Models;

[Table("post")]
public class Post : Record
{
    [Column("title")]
    public string? Title { get; set; }

    [Column("content")]
    public string? Content { get; set; }

    [Column("status")]
    [CborIgnoreIfDefault]
    public string? Status { get; set; }

    [Column("created_at")]
    [CborIgnoreIfDefault]
    public DateTime? CreatedAt { get; set; }
}
