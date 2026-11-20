using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Ws;

namespace SurrealDb.Net.Internals.Cbor.Converters;

/// <summary>
/// Writes a <see cref="SurrealDbWsRequest"/> without the reflection-based object converter,
/// so that sending an RPC request works under Native AOT.
/// </summary>
internal sealed class SurrealDbWsRequestConverter : CborConverterBase<SurrealDbWsRequest>
{
    private const string IdPropertyName = "id";
    private const string MethodPropertyName = "method";
    private const string ParamsPropertyName = "params";
    private const string SessionPropertyName = "session";
    private const string TransactionPropertyName = "txn";

    private readonly ICborConverter<object?> _parameterConverter;
    private readonly ICborConverter<Guid> _guidConverter;

    public SurrealDbWsRequestConverter(CborOptions options)
    {
        _parameterConverter = options.Registry.ConverterRegistry.Lookup<object?>();
        _guidConverter = options.Registry.ConverterRegistry.Lookup<Guid>();
    }

    public override SurrealDbWsRequest Read(ref CborReader reader)
    {
        throw new NotSupportedException($"Cannot read {nameof(SurrealDbWsRequest)} from cbor...");
    }

    public override void Write(ref CborWriter writer, SurrealDbWsRequest value)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        int size =
            2
            + (value.Parameters is null ? 0 : 1)
            + (value.SessionId.HasValue ? 1 : 0)
            + (value.TransactionId.HasValue ? 1 : 0);

        writer.WriteBeginMap(size);

        writer.WriteString(IdPropertyName);
        writer.WriteString(value.Id);

        writer.WriteString(MethodPropertyName);
        writer.WriteString(value.Method);

        if (value.Parameters is not null)
        {
            writer.WriteString(ParamsPropertyName);
            RpcParametersWriter.Write(ref writer, value.Parameters, _parameterConverter);
        }

        if (value.SessionId.HasValue)
        {
            writer.WriteString(SessionPropertyName);
            _guidConverter.Write(ref writer, value.SessionId.Value);
        }

        if (value.TransactionId.HasValue)
        {
            writer.WriteString(TransactionPropertyName);
            _guidConverter.Write(ref writer, value.TransactionId.Value);
        }
    }
}
