using SurrealDb.Net.Models;

namespace SurrealDb.Net.Benchmarks.Models;

public class Product : Record
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> Images { get; set; } = new();
}
