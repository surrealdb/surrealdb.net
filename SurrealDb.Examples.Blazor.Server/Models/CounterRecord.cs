using System.ComponentModel.DataAnnotations.Schema;
using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class CounterRecord : Record
{
    [Column("value")]
    public int Value { get; set; }
}
