using Dahomey.Cbor;
using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.Internals.Ws;

internal class SurrealDbWsOkResponse : ISurrealDbWsStandardResponse
{
    private readonly ReadOnlyMemory<byte>? _binaryResult;
    private readonly CborOptions? _cborOptions;

    public string Id { get; }

    internal SurrealDbWsOkResponse(
        string id,
        ReadOnlyMemory<byte> binaryResult,
        CborOptions cborOptions
    )
    {
        Id = id;
        _binaryResult = binaryResult;
        _cborOptions = cborOptions;
    }

    public T? GetValue<T>()
    {
        return CborSerializer.Deserialize<T>(_binaryResult!.Value.Span, _cborOptions!);
    }

    internal IEnumerable<T> DeserializeEnumerable<T>()
    {
        // TODO : Try to implement yield pattern in the Deserialization
        return CborSerializer.Deserialize<IEnumerable<T>>(_binaryResult!.Value.Span, _cborOptions!);
    }

    internal bool ExpectNone()
    {
        return _binaryResult.HasValue && _binaryResult.Value.Span.ExpectNone();
    }

    internal bool ExpectEmptyArray()
    {
        return _binaryResult.HasValue && _binaryResult.Value.Span.ExpectEmptyArray();
    }
}
