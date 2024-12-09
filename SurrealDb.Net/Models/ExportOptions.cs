using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models;

/// <summary>
/// Settings used to configure the exported data from <see cref="ISurrealDbClient.Export"/> method.
/// </summary>
public class ExportOptions
{
    /// <summary>
    /// Enable or disable exports of users
    /// </summary>
    [CborProperty("users")]
    [CborIgnoreIfDefault]
    public bool? Users { get; set; }

    /// <summary>
    /// Enable or disable exports of accesses
    /// </summary>
    [CborProperty("accesses")]
    [CborIgnoreIfDefault]
    public bool? Accesses { get; set; }

    /// <summary>
    /// Enable or disable exports of params
    /// </summary>
    [CborProperty("params")]
    [CborIgnoreIfDefault]
    public bool? Params { get; set; }

    /// <summary>
    /// Enable or disable exports of functions
    /// </summary>
    [CborProperty("functions")]
    [CborIgnoreIfDefault]
    public bool? Functions { get; set; }

    /// <summary>
    /// Enable or disable exports of versioned records
    /// </summary>
    [CborProperty("versions")]
    [CborIgnoreIfDefault]
    public bool? Versions { get; set; }

    /// <summary>
    /// Enable or disable exports of tables
    /// </summary>
    [CborProperty("tables")]
    [CborIgnoreIfDefault]
    public bool? Tables { get; set; }

    /// <summary>
    /// Enable or disable exports of records
    /// </summary>
    [CborProperty("records")]
    [CborIgnoreIfDefault]
    public bool? Records { get; set; }
}
