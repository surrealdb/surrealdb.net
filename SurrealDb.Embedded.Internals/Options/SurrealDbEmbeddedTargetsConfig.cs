using Dahomey.Cbor.Attributes;

namespace SurrealDb.Embedded.Options;

public sealed class SurrealDbEmbeddedTargetsConfig
{
    /// <summary>
    /// A boolean value to indicate the target configuration is <c>true</c> or <c>false</c>.
    /// </summary>
    [CborProperty("bool")]
    [CborIgnoreIfDefault]
    public bool? Bool { get; internal set; }

    /// <summary>
    /// A list of targets to be allowed or denied.
    /// </summary>
    [CborProperty("array")]
    [CborIgnoreIfDefault]
    public IEnumerable<string>? Array { get; internal set; }
}
