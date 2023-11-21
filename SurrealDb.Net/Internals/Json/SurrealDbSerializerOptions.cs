using SurrealDb.Net.Internals.Json.Converters;
using SurrealDb.Net.Internals.Json.Converters.Spatial;
#if !NET8_0_OR_GREATER
using SurrealDb.Net.Internals.Json.Policies;
#endif
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Json;

internal static class SurrealDbSerializerOptions
{
    private static readonly Lazy<JsonSerializerOptions> _lazy = new(CreateJsonSerializerOptions);

    public static JsonSerializerOptions Default => _lazy.Value;

    public static JsonSerializerOptions CreateJsonSerializerOptions()
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

        var allConverters = defaultConverters
            .Concat(vectorsConverters)
            .Concat(spatialConverters)
            .Concat(dbResponseConverters)
            .ToList();

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

        foreach (var converter in allConverters)
        {
            options.Converters.Add(converter);
        }

        return options;
    }
}
