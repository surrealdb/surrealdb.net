using System.ComponentModel.DataAnnotations.Schema;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Benchmarks.Models;

[Table("address")]
public class Address : Record
{
    [Column("number")]
    public string Number { get; set; } = string.Empty;

    [Column("street")]
    public string Street { get; set; } = string.Empty;

    [Column("city")]
    public string City { get; set; } = string.Empty;

    [Column("country")]
    public string Country { get; set; } = string.Empty;
}
