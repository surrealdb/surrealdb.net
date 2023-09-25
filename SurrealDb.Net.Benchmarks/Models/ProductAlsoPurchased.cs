using SurrealDb.Net.Models;

namespace SurrealDb.Net.Benchmarks.Models;

public class ProductAlsoPurchased
{
	public Thing? Id { get; set; }
	public List<Product> Purchases { get; set; } = new();
}
