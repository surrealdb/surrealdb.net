using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models;

/// <summary>
/// <inheritdoc />
/// </summary>
public class ExportOptionsWithTables : ExportOptions
{
    /// <summary>
    /// Specify the list of tables to export
    /// </summary>
    [CborProperty("tables")]
    public new string[] Tables { get; set; } = [];
}
