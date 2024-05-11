using System.Text.Json.Serialization;
using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Benchmarks.Models;

public class Post : Record
{
    public string? Title { get; set; }
    public string? Content { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [CborIgnoreIfDefault]
    public string? Status { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [CborIgnoreIfDefault]
    public DateTime? CreatedAt { get; set; }
}
