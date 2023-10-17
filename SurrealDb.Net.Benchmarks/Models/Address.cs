using SurrealDb.Net.Models;

namespace SurrealDb.Net.Benchmarks.Models;

public class Address : Record
{
    public string Number { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
