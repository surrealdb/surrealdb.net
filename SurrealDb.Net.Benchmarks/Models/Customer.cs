using System.ComponentModel.DataAnnotations.Schema;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Benchmarks.Models;

[Table("customer")]
public class Customer : Record
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("address")]
    public RecordId? Address { get; set; }
}
