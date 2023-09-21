using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDB.NET.Geographic;

namespace SurrealDB.NET.Json.Converters;

public sealed class PointJsonConverter : JsonConverter<Point>
{
	private static Point ReadFromJsonObject(ref Utf8JsonReader reader)
	{
		if (!reader.Read() || reader.TokenType is not JsonTokenType.PropertyName || !reader.ValueTextEquals("type"u8))
			throw new JsonException("Failed to deserialize JSON to geographic Point: expected a JSON property named 'type'");

		if (!reader.Read() || reader.TokenType is not JsonTokenType.String || !reader.ValueTextEquals("Point"u8))
			throw new JsonException("Failed to deserialize JSON to geographic Point: expected a JSON string value of 'Point'");

		if (!reader.Read() || reader.TokenType is not JsonTokenType.PropertyName || !reader.ValueTextEquals("coordinates"u8))
			throw new JsonException("Failed to deserialize JSON to geographic Point: expected a JSON property named 'coordinates'");

		if (!reader.Read() || reader.TokenType is not JsonTokenType.StartArray)
			throw new JsonException("Failed to deserialize JSON to geographic Point: expected the start of a JSON array");

		var (longitude, latitude) = ReadCoordinatesFromJsonArray(ref reader, out var altitude);

		if (!reader.Read() || reader.TokenType is not JsonTokenType.EndObject)
			throw new JsonException("Failed to deserialize JSON to geographic Point: expected the end of a JSON object");

		return altitude switch
		{
			null => new Point(longitude, latitude),
			_ => new Point(longitude, latitude, altitude),
		};
	}

	private static (double longitude, double latitude) ReadCoordinatesFromJsonArray(ref Utf8JsonReader reader, out double? altitude)
	{
		if (!reader.Read() || reader.TokenType is not JsonTokenType.Number)
			throw new JsonException("Failed to deserialize JSON to geographic Point: expected a JSON number value for the longitude");

		var longitude = reader.GetDouble();

		if (!reader.Read() || reader.TokenType is not JsonTokenType.Number)
			throw new JsonException("Failed to deserialize JSON to geographic Point: expected a JSON number value for the latitude");

		var latitude = reader.GetDouble();

		if (!reader.Read())
			throw new JsonException("Failed to deserialize JSON to geographic Point: unexpected end of JSON tokens");

		if (reader.TokenType is JsonTokenType.Number)
		{
			altitude = reader.GetDouble();

			if (!reader.Read() || reader.TokenType is not JsonTokenType.EndArray)
				throw new JsonException("Failed to deserialize JSON to geographic Point: expected the end of a JSON array");

			return (longitude, latitude);
		}

		if (reader.TokenType is not JsonTokenType.EndArray)
			throw new JsonException("Failed to deserialize JSON to geographic Point: expected the end of a JSON array and object");

		altitude = null;
		return (longitude, latitude);
	}

	public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is JsonTokenType.StartObject)
			return ReadFromJsonObject(ref reader);

		if (reader.TokenType is JsonTokenType.StartArray)
		{
			var (longitude, latitude) = ReadCoordinatesFromJsonArray(ref reader, out var altitude);
			return altitude switch
			{
				null => new Point(longitude, latitude),
				_ => new Point(longitude, latitude, altitude),
			};
		}

		throw new JsonException("Failed to deserialize JSON to geographic Point: unexpected end of JSON tokens");
	}

	public override void Write([NotNull] Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString("type"u8, "Point"u8);
		writer.WritePropertyName("coordinates"u8);
		writer.WriteStartArray();
		writer.WriteNumberValue(value.Longitude);
		writer.WriteNumberValue(value.Latitude);
		if (value.Altitude is not null)
			writer.WriteNumberValue(value.Altitude.Value);
		writer.WriteEndArray();
		writer.WriteEndObject();
	}
}
