using Microsoft.Spatial;
using SurrealDb.Net.Internals.Constants;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Json.Converters.Spatial;

internal class GeometryMultiPolygonValueConverter : JsonConverter<GeometryMultiPolygon>
{
	public override GeometryMultiPolygon? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.None || reader.TokenType == JsonTokenType.Null)
			return default;

		using var doc = JsonDocument.ParseValue(ref reader);
		var root = doc.RootElement;

		if (root.TryGetProperty(SpatialConverterConstants.TypePropertyName, out var typeProperty))
		{
			var type = typeProperty.GetString();

			if (type == MultiPolygonConverter.TypeValue)
			{
				var coordinatesProperty = root.GetProperty(SpatialConverterConstants.CoordinatesPropertyName);

				if (coordinatesProperty.ValueKind != JsonValueKind.Array)
					throw new JsonException($"Cannot deserialize {nameof(GeometryMultiPolygon)} because coordinates must be an array");

				var geometryBuilder = GeometryFactory.MultiPolygon();

				MultiPolygonConverter.ConstructGeometryMultiPolygon(ref coordinatesProperty, geometryBuilder);

				return geometryBuilder.Build();
			}

			throw new JsonException($"Cannot deserialize {nameof(GeometryMultiPolygon)} because of type \"{type}\"");
		}

		throw new JsonException($"Cannot deserialize {nameof(GeometryMultiPolygon)}");
	}

	public override void Write(Utf8JsonWriter writer, GeometryMultiPolygon value, JsonSerializerOptions options)
	{
		MultiPolygonConverter.WriteGeometryMultiPolygon(writer, value);
	}
}
