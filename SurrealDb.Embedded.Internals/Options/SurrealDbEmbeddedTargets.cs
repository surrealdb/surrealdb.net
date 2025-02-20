using Dahomey.Cbor.Attributes;

namespace SurrealDb.Embedded.Options;

public sealed class SurrealDbEmbeddedTargets
{
    /// <summary>
    /// The <c>Allow</c> targets configuration.
    /// </summary>>
    [CborProperty("allow")]
    [CborIgnoreIfDefault]
    public SurrealDbEmbeddedTargetsConfig? Allow { get; internal set; }

    /// <summary>
    /// The <c>Deny</c> targets configuration.
    /// </summary>>
    [CborProperty("deny")]
    [CborIgnoreIfDefault]
    public SurrealDbEmbeddedTargetsConfig? Deny { get; internal set; }
}
