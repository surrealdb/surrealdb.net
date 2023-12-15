using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Data;

public class CounterRecord : Record
{
    public int Value { get; set; }
}
