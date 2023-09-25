using Microsoft.Spatial;
using SurrealDb.Net.Internals.Constants;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Json.Converters.Spatial;

internal class GeographyPolygonValueConverter : JsonConverter<GeographyPolygon>
{
	public override GeographyPolygon? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.None || reader.TokenType == JsonTokenType.Null)
			return default;

		using var doc = JsonDocument.ParseValue(ref reader);
		var root = doc.RootElement;

		if (root.TryGetProperty(SpatialConverterConstants.TypePropertyName, out var typeProperty))
		{
			var type = typeProperty.GetString();

			if (type == PolygonConverter.TypeValue)
			{
				var coordinatesProperty = root.GetProperty(SpatialConverterConstants.CoordinatesPropertyName);

				if (coordinatesProperty.ValueKind != JsonValueKind.Array)
					throw new JsonException($"Cannot deserialize {nameof(GeographyPolygon)} because coordinates must be an array");

				var geographyBuilder = GeographyFactory.Polygon();

				PolygonConverter.ConstructGeographyPolygon(ref coordinatesProperty, geographyBuilder);

				return geographyBuilder.Build();
			}

			throw new JsonException($"Cannot deserialize {nameof(GeographyPolygon)} because of type \"{type}\"");
		}

		throw new JsonException($"Cannot deserialize {nameof(GeographyPolygon)}");
	}

	public override void Write(Utf8JsonWriter writer, GeographyPolygon value, JsonSerializerOptions options)
	{
		PolygonConverter.WriteGeographyPolygon(writer, value);
	}
}
