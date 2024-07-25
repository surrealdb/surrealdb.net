using Dahomey.Cbor;
using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.Internals.Http;

internal class SurrealDbHttpOkResponse : ISurrealDbHttpResponse
{
    private readonly ReadOnlyMemory<byte>? _binaryResult;
    private readonly CborOptions? _cborOptions;

    internal SurrealDbHttpOkResponse(ReadOnlyMemory<byte> binaryResult, CborOptions cborOptions)
    {
        _binaryResult = binaryResult;
        _cborOptions = cborOptions;
    }

    public T? GetValue<T>()
    {
        return CborSerializer.Deserialize<T>(_binaryResult!.Value.Span, _cborOptions!);
    }

    internal IEnumerable<T> DeserializeEnumerable<T>()
    {
        var items = CborSerializer.Deserialize<IEnumerable<T>>(
            _binaryResult!.Value.Span,
            _cborOptions!
        );
        foreach (var item in items)
        {
            yield return item;
        }
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
