using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models.Errors;

internal sealed class NotAllowedErrorDetail
{
    [CborProperty("name")]
    public string? Name { get; private set; }

    public AuthErrorDetail? Auth { get; private set; }
}
