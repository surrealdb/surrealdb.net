using Dahomey.Cbor;
using SurrealDb.Net.Internals.Cbor.Converters;
using SurrealDb.Net.Internals.Cbor.Converters.Numerics;
using SurrealDb.Net.Internals.Cbor.Converters.Spatial;
using SurrealDb.Net.Internals.Cbor.Mappings;
using SurrealDb.Net.Internals.Errors;
using SurrealDb.Net.Internals.Http;
using SurrealDb.Net.Internals.Ws;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Internals.Cbor;

public static class SurrealDbCborOptions
{
    internal static Lazy<CborOptions> Default => new(CreateCborSerializerOptions(false));
    internal static Lazy<CborOptions> Cbor2Default => new(CreateCborSerializerOptions(true));

    /// <param name="cbor2">
    /// Whether the connection negotiated <c>cbor.2</c>, which carries SurrealDB 4.0's sized numbers.
    /// </param>
    private static CborOptions CreateCborSerializerOptions(bool cbor2)
    {
        var options = new CborOptions
        {
            DefaultNamingConvention = new SurrealDbCborNamingConvention(),
        };

        options.Registry.ObjectMappingConventionRegistry.RegisterProvider(
            new RecordIdObjectMappingConventionProvider()
        );
        options.Registry.ObjectMappingConventionRegistry.RegisterProvider(
            new SizedNumberObjectMappingConventionProvider()
        );

        options.Registry.ConverterRegistry.RegisterConverterProvider(
            new PrimitiveConverterProvider(cbor2)
        );
        options.Registry.ConverterRegistry.RegisterConverterProvider(
            new HashSetConverterProvider()
        );
        options.Registry.ConverterRegistry.RegisterConverterProvider(
            new RecordIdOfConverterProvider()
        );
        options.Registry.ConverterRegistry.RegisterConverterProvider(
            new RangeBoundConverterProvider()
        );
        options.Registry.ConverterRegistry.RegisterConverterProvider(new RangeConverterProvider());
        options.Registry.ConverterRegistry.RegisterConverterProvider(
            new RecordIdRangeConverterProvider()
        );
        options.Registry.ConverterRegistry.RegisterConverterProvider(new VectorConverterProvider());
        options.Registry.ConverterRegistry.RegisterConverterProvider(
            new GeographyConverterProvider()
        );
        options.Registry.ConverterRegistry.RegisterConverterProvider(
            new GeometryConverterProvider()
        );
        options.Registry.ConverterRegistry.RegisterConverterProvider(
            new JsonPatchDocumentConverterProvider()
        );
        RegisterWsEngineConverters(options);
        options.Registry.ConverterRegistry.RegisterConverter(typeof(Tokens), new TokensConverter());

        return options;
    }

    public static CborOptions GetCborSerializerOptions(Action<CborOptions>? configureCborOptions)
    {
        return GetCborSerializerOptions(configureCborOptions, false);
    }

    internal static CborOptions GetCborSerializerOptions(
        Action<CborOptions>? configureCborOptions,
        bool cbor2
    )
    {
        if (configureCborOptions is not null)
        {
            var cborOptions = CreateCborSerializerOptions(cbor2);
            configureCborOptions(cborOptions);

            return cborOptions;
        }

        return cbor2 ? Cbor2Default.Value : Default.Value;
    }

    private static void RegisterWsEngineConverters(CborOptions options)
    {
        options.Registry.ConverterRegistry.RegisterConverter(
            typeof(RpcErrorDetails),
            new RpcErrorDetailsConverter()
        );
        options.Registry.ConverterRegistry.RegisterConverter(
            typeof(ISurrealDbResult),
            new SurrealDbResultConverter(options)
        );
        options.Registry.ConverterRegistry.RegisterConverter(
            typeof(SurrealDbQueryStreamFrame),
            new SurrealDbQueryStreamFrameConverter(options)
        );

        options.Registry.ConverterRegistry.RegisterConverter(
            typeof(RpcErrorResponseContent),
            new RpcErrorResponseContentConverter(options)
        );

        options.Registry.ConverterRegistry.RegisterConverter(
            typeof(ISurrealDbHttpResponse),
            new SurrealDbHttpResponseConverter(options)
        );

        options.Registry.ConverterRegistry.RegisterConverter(
            typeof(SurrealDbWsLiveResponseContent),
            new SurrealDbWsLiveResponseContentConverter(options)
        );
        options.Registry.ConverterRegistry.RegisterConverter(
            typeof(ISurrealDbWsResponse),
            new SurrealDbWsResponseConverter(options)
        );
    }
}
