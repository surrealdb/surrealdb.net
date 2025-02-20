using Dahomey.Cbor.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace SurrealDb.Embedded.Options;

public sealed class SurrealDbEmbeddedOptions
{
    /// <summary>
    /// Enables strict mode of the SurrealDB embedded engine.
    /// </summary>
    [CborProperty("strict")]
    [CborIgnoreIfDefault]
    public bool? StrictMode { get; internal set; }

    /// <summary>
    /// Contains a collection of capabilities that can be configured.
    /// </summary>
    [CborProperty("capabilities")]
    [CborIgnoreIfDefault]
    public SurrealDbEmbeddedCapabilities? Capabilities { get; internal set; }

    public static SurrealDbEmbeddedOptionsBuilder Create()
    {
        return new SurrealDbEmbeddedOptionsBuilder();
    }
}
