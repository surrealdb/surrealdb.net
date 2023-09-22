using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDB.NET.Json.Converters;

public sealed class SurrealThingJsonConverter : JsonConverter<Thing>
{
	public override Thing Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		const byte colon = 0x3a;

		var i = reader.ValueSpan.IndexOf(colon);

		if (i is -1)
			throw new SurrealException($"Can not deserialize JSON {reader.TokenType} to Thing");

		return new Thing
		{
			Table = Encoding.UTF8.GetString(reader.ValueSpan[..i]),
			Id = Encoding.UTF8.GetString(reader.ValueSpan[(i + 1)..])
		};
	}

	public override void Write([NotNull] Utf8JsonWriter writer, Thing value, JsonSerializerOptions options)
	{
		writer.WriteStringValue($"{value.Table.Name}:{value.Id}");
	}
}
