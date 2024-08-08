using SurrealDb.Net.Models;

namespace SurrealDb.Net.Benchmarks.Models;

public class Customer : Record
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RecordId? Address { get; set; }
}
