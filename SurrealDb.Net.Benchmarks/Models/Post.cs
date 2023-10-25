using SurrealDb.Net.Models;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Benchmarks.Models;

public class Post : Record
{
    public string? Title { get; set; }
    public string? Content { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? CreatedAt { get; set; }
}
