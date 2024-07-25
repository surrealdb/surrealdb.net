using Dahomey.Cbor;

namespace SurrealDb.Net.Internals.Ws;

internal class SurrealDbWsLiveResponse : ISurrealDbWsLiveResponse
{
    public SurrealDbWsLiveResponseContent Result { get; }

    internal SurrealDbWsLiveResponse(SurrealDbWsLiveResponseContent result)
    {
        Result = result;
    }
}

internal class SurrealDbWsLiveResponseContent
{
    private readonly ReadOnlyMemory<byte>? _binaryResult;
    private readonly CborOptions? _cborOptions;

    public Guid Id { get; }

    public string Action { get; }

    internal SurrealDbWsLiveResponseContent(
        Guid id,
        string action,
        ReadOnlyMemory<byte> binaryResult,
        CborOptions cborOptions
    )
    {
        Id = id;
        Action = action;
        _binaryResult = binaryResult;
        _cborOptions = cborOptions;
    }

    public T? GetValue<T>()
    {
        return CborSerializer.Deserialize<T>(_binaryResult!.Value.Span, _cborOptions!);
    }
}
