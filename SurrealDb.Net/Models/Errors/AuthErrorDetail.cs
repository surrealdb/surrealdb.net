using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models.Errors;

internal sealed class AuthErrorDetail
{
    [CborProperty("kind")]
    public string? Kind { get; private set; }

    [CborProperty("name")]
    public string? Name { get; private set; }

    [CborProperty("actor")]
    public string? Actor { get; private set; }

    [CborProperty("action")]
    public string? Action { get; private set; }

    [CborProperty("resource")]
    public string? Resource { get; private set; }
}
