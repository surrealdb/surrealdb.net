using System.Collections.Concurrent;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Conventions;
using SurrealDb.Net.Internals.Cbor.Converters;
using SurrealDb.Net.Internals.Cbor.Converters.Numerics;
using SurrealDb.Net.Internals.Cbor.Converters.Spatial;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Http;
using SurrealDb.Net.Internals.Ws;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Internals.Cbor;

public static class SurrealDbCborOptions
{
    private static readonly string DefaultKey = string.Empty;

    private static readonly ConcurrentDictionary<
        string,
        CborOptions
    > DefaultSerializerOptionsCache = new();

    internal static CborOptions Default =>
        DefaultSerializerOptionsCache.GetValueOrDefault(
            DefaultKey,
            CreateCborSerializerOptions(null)
        )!;

    private static CborOptions CreateCborSerializerOptions(INamingConvention? namingConvention)
    {
        var options = new CborOptions { DefaultNamingConvention = namingConvention };

        options.Registry.ConverterRegistry.RegisterConverterProvider(
            new PrimitiveConverterProvider()
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

        return options;
    }

    private static (string key, INamingConvention? namingConvention) DetectNamingConvention(
        string? namingPolicy
    )
    {
        INamingConvention? namingConvention = (namingPolicy?.ToLowerInvariant()) switch
        {
            NamingPolicyConstants.CAMEL_CASE => new CamelCaseNamingConvention(),
            NamingPolicyConstants.SNAKE_CASE
            or NamingPolicyConstants.SNAKE_CASE_LOWER
                => new SnakeCaseNamingConvention(),
            NamingPolicyConstants.SNAKE_CASE_UPPER => new UpperSnakeCaseNamingConvention(),
            NamingPolicyConstants.KEBAB_CASE
            or NamingPolicyConstants.KEBAB_CASE_LOWER
                => new KebabCaseNamingConvention(),
            NamingPolicyConstants.KEBAB_CASE_UPPER => new UpperKebabCaseNamingConvention(),
            _ => null,
        };

        string key = namingConvention?.GetType().FullName ?? DefaultKey;

        return (key, namingConvention);
    }

    private static readonly ConcurrentDictionary<
        string,
        (string key, INamingConvention? namingConvention)
    > DetectedNamingConventions = new();

    public static CborOptions GetCborSerializerOptions(
        string? namingPolicy,
        Action<CborOptions>? configureCborOptions
    )
    {
        var (key, jsonNamingPolicy) = DetectedNamingConventions.GetOrAdd(
            namingPolicy ?? string.Empty,
            DetectNamingConvention(namingPolicy)
        );

        if (configureCborOptions is not null)
        {
            var cborOptions = CreateCborSerializerOptions(jsonNamingPolicy);
            cborOptions.EnumFormat = ValueFormat.WriteToString;
            configureCborOptions(cborOptions);

            return cborOptions;
        }

        return GetDefaultSerializerFromPolicy(key, jsonNamingPolicy);
    }

    private static CborOptions GetDefaultSerializerFromPolicy(INamingConvention? namingConvention)
    {
        string key = namingConvention?.GetType().FullName ?? DefaultKey;
        return GetDefaultSerializerFromPolicy(key, namingConvention);
    }

    private static CborOptions GetDefaultSerializerFromPolicy(
        string key,
        INamingConvention? namingConvention
    )
    {
        return DefaultSerializerOptionsCache.GetOrAdd(
            key,
            CreateCborSerializerOptions(namingConvention)
        );
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
