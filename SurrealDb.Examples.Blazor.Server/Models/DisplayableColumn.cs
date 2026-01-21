using System.ComponentModel.DataAnnotations.Schema;
using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class DisplayableColumn : Record
{
    internal const string Table = "displayable_column";

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("order")]
    public int Order { get; set; }

    [Column("tasks")]
    public IEnumerable<DisplayableTask> Tasks { get; set; } = [];
}
