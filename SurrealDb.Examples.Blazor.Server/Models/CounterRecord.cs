using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class CounterRecord : Record
{
    public int Value { get; set; }
}
