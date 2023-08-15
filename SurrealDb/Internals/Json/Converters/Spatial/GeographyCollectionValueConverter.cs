using Microsoft.Spatial;
using SurrealDb.Internals.Constants;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Internals.Json.Converters.Spatial;

internal class GeographyCollectionValueConverter : JsonConverter<GeographyCollection>
{
	public override GeographyCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.None || reader.TokenType == JsonTokenType.Null)
			return default;

		using var doc = JsonDocument.ParseValue(ref reader);
		var root = doc.RootElement;

		if (root.TryGetProperty(SpatialConverterConstants.TypePropertyName, out var typeProperty))
		{
			var type = typeProperty.GetString();

			if (type == CollectionConverter.TypeValue)
			{
				var geometriesProperty = root.GetProperty(SpatialConverterConstants.GeometriesPropertyName);

				if (geometriesProperty.ValueKind != JsonValueKind.Array)
					throw new JsonException($"Cannot deserialize {nameof(GeographyCollection)} because coordinates must be an array");

				var geographyBuilder = GeographyFactory.Collection();

				foreach (var geometryProperty in geometriesProperty.EnumerateArray())
				{
					var geometryType = geometryProperty.GetProperty(SpatialConverterConstants.TypePropertyName).GetString();
					var coordinatesProperty = geometryProperty.GetProperty(SpatialConverterConstants.CoordinatesPropertyName);

					switch (geometryType)
					{
						case PointConverter.TypeValue:
							PointConverter.ConstructGeographyPoint(ref coordinatesProperty, geographyBuilder);
							break;
						case LineStringConverter.TypeValue:
							geographyBuilder.LineString();
							LineStringConverter.ConstructGeographyLineString(ref coordinatesProperty, geographyBuilder);
							break;
						case PolygonConverter.TypeValue:
							geographyBuilder.Polygon();
							PolygonConverter.ConstructGeographyPolygon(ref coordinatesProperty, geographyBuilder);
							break;
						case MultiPointConverter.TypeValue:
							geographyBuilder.MultiPoint();
							MultiPointConverter.ConstructGeographyMultiPoint(ref coordinatesProperty, geographyBuilder);
							break;
						case MultiLineStringConverter.TypeValue:
							geographyBuilder.MultiLineString();
							MultiLineStringConverter.ConstructGeographyMultiLineString(ref coordinatesProperty, geographyBuilder);
							break;
						case MultiPolygonConverter.TypeValue:
							geographyBuilder.MultiPolygon();
							MultiPolygonConverter.ConstructGeographyMultiPolygon(ref coordinatesProperty, geographyBuilder);
							break;
						default:
							throw new JsonException($"Cannot deserialize {nameof(GeographyCollection)}. Unknown geometry type \"{geometryType}\"");
					}
				}

				return geographyBuilder.Build();
			}

			throw new JsonException($"Cannot deserialize {nameof(GeographyCollection)} because of type \"{type}\"");
		}

		throw new JsonException($"Cannot deserialize {nameof(GeographyCollection)}");
	}

	public override void Write(Utf8JsonWriter writer, GeographyCollection value, JsonSerializerOptions options)
	{
		if (value is null)
		{
			writer.WriteNullValue();
			return;
		}

		writer.WriteStartObject();

		writer.WritePropertyName(SpatialConverterConstants.TypePropertyName);
		writer.WriteStringValue(CollectionConverter.TypeValue);

		writer.WritePropertyName(SpatialConverterConstants.GeometriesPropertyName);
		writer.WriteStartArray();

		foreach (var geography in value.Geographies)
		{
			switch (geography)
			{
				case GeographyPoint point:
					PointConverter.WriteGeographyPoint(writer, point);
					break;
				case GeographyLineString lineString:
					LineStringConverter.WriteGeographyLineString(writer, lineString);
					break;
				case GeographyPolygon polygon:
					PolygonConverter.WriteGeographyPolygon(writer, polygon);
					break;
				case GeographyMultiPoint multiPoint:
					MultiPointConverter.WriteGeographyMultiPoint(writer, multiPoint);
					break;
				case GeographyMultiLineString multiLineString:
					MultiLineStringConverter.WriteGeographyMultiLineString(writer, multiLineString);
					break;
				case GeographyMultiPolygon multiPolygon:
					MultiPolygonConverter.WriteGeographyMultiPolygon(writer, multiPolygon);
					break;
				default:
					throw new JsonException($"Cannot serialize {nameof(GeographyCollection)}. Unknown geometry type \"{geography.GetType().Name}\"");
			}
		}

		writer.WriteEndArray();

		writer.WriteEndObject();
	}
}
