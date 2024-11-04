using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Benchmarks.Models;

public class Post : Record
{
    public string? Title { get; set; }
    public string? Content { get; set; }

    [CborIgnoreIfDefault]
    public string? Status { get; set; }

    [CborIgnoreIfDefault]
    public DateTime? CreatedAt { get; set; }
}
