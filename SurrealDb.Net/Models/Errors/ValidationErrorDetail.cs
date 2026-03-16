using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models.Errors;

internal sealed class ValidationErrorDetail
{
    [CborProperty("name")]
    public string? Name { get; private set; }

    [CborProperty("value")]
    public string? Value { get; private set; }
}
