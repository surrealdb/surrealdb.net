using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class DisplayableColumn : Record
{
    internal const string Table = "displayable_column";

    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public IEnumerable<DisplayableTask> Tasks { get; set; } = Array.Empty<DisplayableTask>();
}
