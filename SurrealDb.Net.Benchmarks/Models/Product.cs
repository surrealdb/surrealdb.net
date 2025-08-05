using System.ComponentModel.DataAnnotations.Schema;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Benchmarks.Models;

[Table("product")]
public class Product : Record
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("price")]
    public decimal Price { get; set; }

    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Column("images")]
    public List<string> Images { get; set; } = new();
}
