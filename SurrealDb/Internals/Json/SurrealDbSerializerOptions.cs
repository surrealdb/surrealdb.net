using SurrealDb.Internals.Json.Converters;
using SurrealDb.Internals.Json.Policies;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Internals.Json;

internal static class SurrealDbSerializerOptions
{
    private static readonly Lazy<JsonSerializerOptions> _lazy = new(CreateJsonSerializerOptions);

    public static JsonSerializerOptions Default => _lazy.Value;

    public static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        return new()
        {
            AllowTrailingCommas = true,
            Converters =
            {
                new ThingConverter(),
                new TimeSpanValueConverter(),
                new DateTimeValueConverter(),
#if NET6_0_OR_GREATER
				new DateOnlyValueConverter(),
				new TimeOnlyValueConverter(),
#endif
				new SurrealDbResultConverter(),
				new SurrealDbWsResponseConverter(),
			},
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };
    }
}
