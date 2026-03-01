using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models.Errors;

internal sealed class AlreadyExistsErrorDetail
{
    [CborProperty("id")]
    public string? Id { get; private set; }

    [CborProperty("name")]
    public string? Name { get; private set; }
}
