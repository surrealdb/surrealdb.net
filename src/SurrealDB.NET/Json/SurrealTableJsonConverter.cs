using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDB.NET.Json;

public sealed class SurrealTableJsonConverter : JsonConverter<Table>
{
	public override Table Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.String)
			throw new SurrealException($"Can not deserialize JSON {reader.TokenType} to Table");

		return new Table
		{
			Name = Encoding.UTF8.GetString(reader.ValueSpan),
		};
	}

	public override void Write([NotNull] Utf8JsonWriter writer, Table value, JsonSerializerOptions options)
	{
		writer.WriteStringValue($"{value.Name}");
	}
}
