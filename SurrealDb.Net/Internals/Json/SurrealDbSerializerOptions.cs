using SurrealDb.Net.Internals.Json.Converters;
using SurrealDb.Net.Internals.Json.Converters.Spatial;
#if !NET8_0_OR_GREATER
using SurrealDb.Net.Internals.Json.Policies;
#endif
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Internals.Models;

namespace SurrealDb.Net.Internals.Json;

internal static class SurrealDbSerializerOptions
{
    private static readonly Lazy<JsonSerializerOptions> _lazy = new(CreateJsonSerializerOptions);

    public static JsonSerializerOptions Default => _lazy.Value;

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            NumberHandling =
                JsonNumberHandling.AllowReadingFromString
                | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            PropertyNameCaseInsensitive = true,
#if NET8_0_OR_GREATER
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
#else
            PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
#endif
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

        return defaultConverters
            .Concat(vectorsConverters)
            .Concat(spatialConverters)
            .Concat(dbResponseConverters);
    }

    public static JsonSerializerOptions GetJsonSerializerOptions(
#if NET8_0_OR_GREATER
        JsonSerializerContext mainContext,
#endif
        Action<JsonSerializerOptions>? configureJsonSerializerOptions,
        Func<JsonSerializerContext[]>? prependJsonSerializerContexts,
        Func<JsonSerializerContext[]>? appendJsonSerializerContexts,
        CurrentJsonSerializerOptionsForAot? currentJsonSerializerOptionsForAot,
        out CurrentJsonSerializerOptionsForAot? updatedJsonSerializerOptionsForAot
    )
    {
        updatedJsonSerializerOptionsForAot = null;

#if NET8_0_OR_GREATER
        if (prependJsonSerializerContexts is not null || appendJsonSerializerContexts is not null)
        {
            JsonSerializerOptions jsonSerializerOptions;

            var jsonSerializerContextsToPrepend = prependJsonSerializerContexts?.Invoke();
            var jsonSerializerContextsToAppend = appendJsonSerializerContexts?.Invoke();

            if (configureJsonSerializerOptions is not null)
            {
                jsonSerializerOptions = CreateJsonSerializerOptions();
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

                jsonSerializerOptions = CreateJsonSerializerOptions();
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
#else
        if (configureJsonSerializerOptions is not null)
        {
            var jsonSerializerOptions = CreateJsonSerializerOptions();
            configureJsonSerializerOptions(jsonSerializerOptions);

            return jsonSerializerOptions;
        }
#endif

        return Default;
    }
}
