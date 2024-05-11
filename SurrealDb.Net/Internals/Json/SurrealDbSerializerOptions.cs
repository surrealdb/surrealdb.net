using System;
using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Json.Converters;
using SurrealDb.Net.Internals.Json.Converters.Spatial;
using SurrealDb.Net.Internals.Models;
using TupleAsJsonArray;

namespace SurrealDb.Net.Internals.Json;

internal static class SurrealDbSerializerOptions
{
    private static readonly string DefaultKey = string.Empty;

    private static readonly ConcurrentDictionary<
        string,
        JsonSerializerOptions
    > DefaultSerializerOptionsCache = new();

    public static JsonSerializerOptions Default =>
        DefaultSerializerOptionsCache.GetValueOrDefault(
            DefaultKey,
            CreateJsonSerializerOptions(null)
        )!;

    private static JsonSerializerOptions CreateJsonSerializerOptions(
        JsonNamingPolicy? jsonNamingPolicy
    )
    {
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            NumberHandling =
                JsonNumberHandling.AllowReadingFromString
                | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = jsonNamingPolicy,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        AddAllJsonConverters(options);

        return options;
    }

    private static void AddAllJsonConverters(JsonSerializerOptions options)
    {
        foreach (var converter in GetAllJsonConverters())
        {
            options.Converters.Add(converter);
        }
    }

    private static IEnumerable<JsonConverter> GetAllJsonConverters()
    {
        var defaultConverters = new List<JsonConverter>
        {
            new ThingConverter(),
            new DurationConverter(),
            new TimeSpanValueConverter(),
            new DateTimeValueConverter(),
#if NET6_0_OR_GREATER
            new DateOnlyValueConverter(),
            new TimeOnlyValueConverter(),
#endif
        };

        var vectorsConverters = new List<JsonConverter>
        {
            new Vector2ValueConverter(),
            new Vector3ValueConverter(),
            new Vector4ValueConverter(),
        };

        var spatialConverters = new List<JsonConverter>
        {
            new GeometryPointValueConverter(),
            new GeometryLineStringValueConverter(),
            new GeometryPolygonValueConverter(),
            new GeometryMultiPointValueConverter(),
            new GeometryMultiLineStringValueConverter(),
            new GeometryMultiPolygonValueConverter(),
            new GeometryCollectionValueConverter(),
            new GeographyPointValueConverter(),
            new GeographyLineStringValueConverter(),
            new GeographyPolygonValueConverter(),
            new GeographyMultiPointValueConverter(),
            new GeographyMultiLineStringValueConverter(),
            new GeographyMultiPolygonValueConverter(),
            new GeographyCollectionValueConverter(),
        };

        var dbResponseConverters = new List<JsonConverter>
        {
            new SurrealDbResultConverter(),
            new SurrealDbWsResponseConverter(),
        };

        var extraConverters = new List<JsonConverter> { new TupleConverterFactory(), };

        return defaultConverters
            .Concat(vectorsConverters)
            .Concat(spatialConverters)
            .Concat(dbResponseConverters)
            .Concat(extraConverters);
    }

    private static (string key, JsonNamingPolicy? jsonNamingPolicy) DetectJsonNamingPolicy(
        string? namingPolicy
    )
    {
        var jsonNamingPolicy = (namingPolicy?.ToLowerInvariant()) switch
        {
            NamingPolicyConstants.CAMEL_CASE => JsonNamingPolicy.CamelCase,
            NamingPolicyConstants.SNAKE_CASE
            or NamingPolicyConstants.SNAKE_CASE_LOWER
                => JsonNamingPolicy.SnakeCaseLower,
            NamingPolicyConstants.SNAKE_CASE_UPPER => JsonNamingPolicy.SnakeCaseUpper,
            NamingPolicyConstants.KEBAB_CASE
            or NamingPolicyConstants.KEBAB_CASE_LOWER
                => JsonNamingPolicy.KebabCaseLower,
            NamingPolicyConstants.KEBAB_CASE_UPPER => JsonNamingPolicy.KebabCaseUpper,
            _ => null,
        };

        string key = jsonNamingPolicy?.GetType().FullName ?? DefaultKey;

        return (key, jsonNamingPolicy);
    }

    private static readonly ConcurrentDictionary<
        string,
        (string key, JsonNamingPolicy? jsonNamingPolicy)
    > DetectedJsonNamingPolicies = new();

    public static JsonSerializerOptions GetJsonSerializerOptions(
#if NET8_0_OR_GREATER
        JsonSerializerContext mainContext,
#endif
        string? namingPolicy,
        Action<JsonSerializerOptions>? configureJsonSerializerOptions,
        Func<JsonSerializerContext[]>? prependJsonSerializerContexts,
        Func<JsonSerializerContext[]>? appendJsonSerializerContexts,
        CurrentJsonSerializerOptionsForAot? currentJsonSerializerOptionsForAot,
        out CurrentJsonSerializerOptionsForAot? updatedJsonSerializerOptionsForAot
    )
    {
        updatedJsonSerializerOptionsForAot = null;

        var (key, jsonNamingPolicy) = DetectedJsonNamingPolicies.GetOrAdd(
            namingPolicy ?? string.Empty,
            DetectJsonNamingPolicy(namingPolicy)
        );

#if NET8_0_OR_GREATER
        if (prependJsonSerializerContexts is not null || appendJsonSerializerContexts is not null)
        {
            JsonSerializerOptions jsonSerializerOptions;

            var jsonSerializerContextsToPrepend = prependJsonSerializerContexts?.Invoke();
            var jsonSerializerContextsToAppend = appendJsonSerializerContexts?.Invoke();

            if (configureJsonSerializerOptions is not null)
            {
                jsonSerializerOptions = CreateJsonSerializerOptions(jsonNamingPolicy);
                AddAllJsonConverters(jsonSerializerOptions);

                configureJsonSerializerOptions(jsonSerializerOptions);
            }
            else
            {
                if (
                    currentJsonSerializerOptionsForAot is not null
                    && currentJsonSerializerOptionsForAot.Equals(
                        jsonSerializerContextsToPrepend,
                        jsonSerializerContextsToAppend
                    )
                )
                {
                    return currentJsonSerializerOptionsForAot.Options;
                }

                jsonSerializerOptions = CreateJsonSerializerOptions(jsonNamingPolicy);
                AddAllJsonConverters(jsonSerializerOptions);
            }

            if (jsonSerializerContextsToPrepend is not null)
            {
                foreach (var jsonSerializerContext in jsonSerializerContextsToPrepend)
                {
                    jsonSerializerOptions.TypeInfoResolverChain.Add(jsonSerializerContext);
                }
            }

            jsonSerializerOptions.TypeInfoResolverChain.Add(mainContext);

            if (jsonSerializerContextsToAppend is not null)
            {
                foreach (var jsonSerializerContext in jsonSerializerContextsToAppend)
                {
                    jsonSerializerOptions.TypeInfoResolverChain.Add(jsonSerializerContext);
                }
            }

            updatedJsonSerializerOptionsForAot = new CurrentJsonSerializerOptionsForAot(
                jsonSerializerContextsToPrepend,
                jsonSerializerContextsToAppend,
                jsonSerializerOptions
            );

            return jsonSerializerOptions;
        }
#endif

        if (configureJsonSerializerOptions is not null)
        {
            var jsonSerializerOptions = CreateJsonSerializerOptions(jsonNamingPolicy);
            configureJsonSerializerOptions(jsonSerializerOptions);

            return jsonSerializerOptions;
        }

        return GetDefaultSerializerFromPolicy(key, jsonNamingPolicy);
    }

    public static JsonSerializerOptions GetDefaultSerializerFromPolicy(
        JsonNamingPolicy? jsonNamingPolicy
    )
    {
        string key = jsonNamingPolicy?.GetType().FullName ?? DefaultKey;
        return GetDefaultSerializerFromPolicy(key, jsonNamingPolicy);
    }

    private static JsonSerializerOptions GetDefaultSerializerFromPolicy(
        string key,
        JsonNamingPolicy? jsonNamingPolicy
    )
    {
        return DefaultSerializerOptionsCache.GetOrAdd(
            key,
            CreateJsonSerializerOptions(jsonNamingPolicy)
        );
    }
}
