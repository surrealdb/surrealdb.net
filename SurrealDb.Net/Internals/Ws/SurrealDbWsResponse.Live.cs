using Dahomey.Cbor;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Ws;

internal sealed class SurrealDbWsLiveResponse : ISurrealDbWsLiveResponse
{
    public SurrealDbWsLiveResponseContent Result { get; }

    internal SurrealDbWsLiveResponse(SurrealDbWsLiveResponseContent result)
    {
        Result = result;
    }
}

internal sealed class SurrealDbWsLiveResponseContent
{
    private readonly ReadOnlyMemory<byte>? _binaryResult;
    private readonly CborOptions? _cborOptions;

    public Guid Id { get; }

    public string Action { get; }

    public RecordId? Record { get; }

    internal SurrealDbWsLiveResponseContent(
        Guid id,
        string action,
        ReadOnlyMemory<byte> binaryResult,
        RecordId? record,
        CborOptions cborOptions
    )
    {
        Id = id;
        Action = action;
        _binaryResult = binaryResult;
        Record = record;
        _cborOptions = cborOptions;
    }

    public T? GetValue<T>()
    {
        return CborSerializer.Deserialize<T>(_binaryResult!.Value.Span, _cborOptions!);
    }
}
