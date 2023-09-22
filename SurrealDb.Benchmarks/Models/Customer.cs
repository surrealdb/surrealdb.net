using SurrealDb.Models;

namespace SurrealDb.Benchmarks.Models;

public class Customer : Record
{
	public string Name { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public Thing? Address { get; set; }
}
