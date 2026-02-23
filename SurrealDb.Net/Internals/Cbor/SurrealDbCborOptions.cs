using Dahomey.Cbor;
using SurrealDb.Net.Internals.Cbor.Converters;
using SurrealDb.Net.Internals.Cbor.Converters.Numerics;
using SurrealDb.Net.Internals.Cbor.Converters.Spatial;
using SurrealDb.Net.Internals.Http;
using SurrealDb.Net.Internals.Ws;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Internals.Cbor;

public static class SurrealDbCborOptions
{
    internal static Lazy<CborOptions> Default => new(CreateCborSerializerOptions());

    private static CborOptions CreateCborSerializerOptions()
    {
        var options = new CborOptions
        {
            DefaultNamingConvention = new SurrealDbCborNamingConvention(),
        };

        options.Registry.ConverterRegistry.RegisterConverterProvider(
            new PrimitiveConverterProvider()
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
        if (configureCborOptions is not null)
        {
            var cborOptions = CreateCborSerializerOptions();
            configureCborOptions(cborOptions);

            return cborOptions;
        }

        return Default.Value;
    }

    private static void RegisterWsEngineConverters(CborOptions options)
    {
        options.Registry.ConverterRegistry.RegisterConverter(
            typeof(ISurrealDbResult),
            new SurrealDbResultConverter(options)
        );

        options.Registry.ConverterRegistry.RegisterConverter(
            typeof(SurrealDbHttpErrorResponseContent),
            new SurrealDbHttpErrorResponseContentConverter()
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
            typeof(SurrealDbWsErrorResponseContent),
            new SurrealDbWsErrorResponseContentConverter()
        );
        options.Registry.ConverterRegistry.RegisterConverter(
            typeof(ISurrealDbWsResponse),
            new SurrealDbWsResponseConverter(options)
        );
    }
}
