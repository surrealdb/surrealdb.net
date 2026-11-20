using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Cbor.Converters;
using SurrealDb.Net.Internals.Cbor.Converters.Auth;
using SurrealDb.Net.Internals.Cbor.Converters.Numerics;
using SurrealDb.Net.Internals.Cbor.Converters.Spatial;
using SurrealDb.Net.Internals.Cbor.Mappings;
using SurrealDb.Net.Internals.Errors;
using SurrealDb.Net.Internals.Http;
using SurrealDb.Net.Internals.Ws;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Internals.Cbor;

public static class SurrealDbCborOptions
{
    internal static readonly Lazy<CborOptions> Default = new(CreateCborSerializerOptions);

    private static CborOptions CreateCborSerializerOptions()
    {
        var options = new CborOptions
        {
            DefaultNamingConvention = new SurrealDbCborNamingConvention(),
        };

        options.Registry.ObjectMappingConventionRegistry.RegisterProvider(
            new RecordIdObjectMappingConventionProvider()
        );

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
        RegisterRequestConverters(options);

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
            typeof(RpcErrorDetails),
            new RpcErrorDetailsConverter()
        );
        options.Registry.ConverterRegistry.RegisterConverter(
            typeof(ISurrealDbResult),
            new SurrealDbResultConverter(options)
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
        options.Registry.ConverterRegistry.RegisterConverter(
            typeof(List<ISurrealDbResult>),
            new CollectionConverter<List<ISurrealDbResult>, ISurrealDbResult>(options)
        );
    }

    /// <summary>
    /// Registers explicitly constructed converters for every type the library sends to the server.
    /// The reflection-based object converter, and the providers that build generic converters with
    /// <c>MakeGenericType</c>, are never reached for them, so sending a request works under Native AOT.
    /// </summary>
    private static void RegisterRequestConverters(CborOptions options)
    {
        var converters = options.Registry.ConverterRegistry;

        // Registered first: the converters below look it up when they are constructed.
        converters.RegisterConverter(typeof(object), new UntypedObjectConverter(options));
        converters.RegisterConverter(typeof(object[]), new ArrayConverter<object?>(options));

        converters.RegisterConverter(
            typeof(SurrealDbWsRequest),
            new SurrealDbWsRequestConverter(options)
        );
        converters.RegisterConverter(
            typeof(SurrealDbHttpRequest),
            new SurrealDbHttpRequestConverter(options)
        );

        converters.RegisterConverter(typeof(RootAuth), new RootAuthConverter());
        converters.RegisterConverter(typeof(NamespaceAuth), new NamespaceAuthConverter());
        converters.RegisterConverter(typeof(DatabaseAuth), new DatabaseAuthConverter());
        converters.RegisterConverter(typeof(ScopeAuth), new ScopeAuthConverter(options));

        converters.RegisterConverter(typeof(ExportOptions), new ExportOptionsConverter(options));
    }
}
