using SurrealDb.Models;

namespace SurrealDb.Benchmarks.Models;

public class Post : Record
{
	public string? Title { get; set; }
	public string? Content { get; set; }
	public string? Status { get; set; }
	public DateTime CreatedAt { get; set; }
}
