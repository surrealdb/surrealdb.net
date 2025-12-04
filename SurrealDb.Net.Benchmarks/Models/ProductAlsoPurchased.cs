using System.ComponentModel.DataAnnotations.Schema;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Benchmarks.Models;

public class ProductAlsoPurchased
{
    [Column("id")]
    public RecordId? Id { get; set; }

    [Column("purchases")]
    public List<Product> Purchases { get; set; } = new();
}
