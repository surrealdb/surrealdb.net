using Dahomey.Cbor.Attributes;

namespace SurrealDb.Embedded.Options;

public sealed class SurrealDbEmbeddedCapabilities
{
    /// <summary>
    /// Targets configuration for Experimental features.
    /// </summary>
    [CborProperty("experimental")]
    [CborIgnoreIfDefault]
    public SurrealDbEmbeddedTargets? Experimental { get; internal set; }
}
