using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models.Errors;

internal sealed class QueryErrorDetail
{
    [CborProperty("secs")]
    public int? Seconds { get; private set; }

    [CborProperty("nanos")]
    public int? Nanos { get; private set; }
}
