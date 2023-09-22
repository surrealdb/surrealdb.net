using SurrealDb.Models;

namespace SurrealDb.Benchmarks.Models;

public class ProductAlsoPurchased
{
	public Thing? Id { get; set; }
	public List<Product> Purchases { get; set; } = new();
}
