using Microsoft.Spatial;
using SurrealDb.Net.Internals.Constants;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Json.Converters.Spatial;

internal class GeometryCollectionValueConverter : JsonConverter<GeometryCollection>
{
	public override GeometryCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
					throw new JsonException($"Cannot deserialize {nameof(GeometryCollection)} because coordinates must be an array");

				var geometryBuilder = GeometryFactory.Collection();

				foreach (var geometryProperty in geometriesProperty.EnumerateArray())
				{
					var geometryType = geometryProperty.GetProperty(SpatialConverterConstants.TypePropertyName).GetString();
					var coordinatesProperty = geometryProperty.GetProperty(SpatialConverterConstants.CoordinatesPropertyName);

					switch (geometryType) {
						case PointConverter.TypeValue:
							PointConverter.ConstructGeometryPoint(ref coordinatesProperty, geometryBuilder);
							break;
						case LineStringConverter.TypeValue:
							geometryBuilder.LineString();
							LineStringConverter.ConstructGeometryLineString(ref coordinatesProperty, geometryBuilder);
							break;
						case PolygonConverter.TypeValue:
							geometryBuilder.Polygon();
							PolygonConverter.ConstructGeometryPolygon(ref coordinatesProperty, geometryBuilder);
							break;
						case MultiPointConverter.TypeValue:
							geometryBuilder.MultiPoint();
							MultiPointConverter.ConstructGeometryMultiPoint(ref coordinatesProperty, geometryBuilder);
							break;
						case MultiLineStringConverter.TypeValue:
							geometryBuilder.MultiLineString();
							MultiLineStringConverter.ConstructGeometryMultiLineString(ref coordinatesProperty, geometryBuilder);
							break;
						case MultiPolygonConverter.TypeValue:
							geometryBuilder.MultiPolygon();
							MultiPolygonConverter.ConstructGeometryMultiPolygon(ref coordinatesProperty, geometryBuilder);
							break;
						default:
							throw new JsonException($"Cannot deserialize {nameof(GeometryCollection)}. Unknown geometry type \"{geometryType}\"");
					}
				}

				return geometryBuilder.Build();
			}

			throw new JsonException($"Cannot deserialize {nameof(GeometryCollection)} because of type \"{type}\"");
		}

		throw new JsonException($"Cannot deserialize {nameof(GeometryCollection)}");
	}

	public override void Write(Utf8JsonWriter writer, GeometryCollection value, JsonSerializerOptions options)
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

		foreach (var geometry in value.Geometries)
		{
			switch (geometry)
			{
				case GeometryPoint point:
					PointConverter.WriteGeometryPoint(writer, point);
					break;
				case GeometryLineString lineString:
					LineStringConverter.WriteGeometryLineString(writer, lineString);
					break;
				case GeometryPolygon polygon:
					PolygonConverter.WriteGeometryPolygon(writer, polygon);
					break;
				case GeometryMultiPoint multiPoint:
					MultiPointConverter.WriteGeometryMultiPoint(writer, multiPoint);
					break;
				case GeometryMultiLineString multiLineString:
					MultiLineStringConverter.WriteGeometryMultiLineString(writer, multiLineString);
					break;
				case GeometryMultiPolygon multiPolygon:
					MultiPolygonConverter.WriteGeometryMultiPolygon(writer, multiPolygon);
					break;
				default:
					throw new JsonException($"Cannot serialize {nameof(GeometryCollection)}. Unknown geometry type \"{geometry.GetType().Name}\"");
			}
		}

		writer.WriteEndArray();

		writer.WriteEndObject();
	}
}
