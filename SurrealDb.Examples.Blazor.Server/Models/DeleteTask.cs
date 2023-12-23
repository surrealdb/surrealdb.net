using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class DeleteTask : Record
{
    internal const string Table = "delete_task";

    public Thing Task { get; set; } = null!;
}
