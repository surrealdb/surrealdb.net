using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Http;

namespace SurrealDb.Net.Internals.Cbor.Converters;

/// <summary>
/// Writes a <see cref="SurrealDbHttpRequest"/> without the reflection-based object converter,
/// so that sending an RPC request works under Native AOT.
/// </summary>
internal sealed class SurrealDbHttpRequestConverter : CborConverterBase<SurrealDbHttpRequest>
{
    private const string MethodPropertyName = "method";
    private const string ParamsPropertyName = "params";
    private const string SessionPropertyName = "session";

    private readonly ICborConverter<object?> _parameterConverter;
    private readonly ICborConverter<Guid> _guidConverter;

    public SurrealDbHttpRequestConverter(CborOptions options)
    {
        _parameterConverter = options.Registry.ConverterRegistry.Lookup<object?>();
        _guidConverter = options.Registry.ConverterRegistry.Lookup<Guid>();
    }

    public override SurrealDbHttpRequest Read(ref CborReader reader)
    {
        throw new NotSupportedException($"Cannot read {nameof(SurrealDbHttpRequest)} from cbor...");
    }

    public override void Write(ref CborWriter writer, SurrealDbHttpRequest value)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        int size = 1 + (value.Parameters is null ? 0 : 1) + (value.SessionId.HasValue ? 1 : 0);

        writer.WriteBeginMap(size);

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
    }
}
