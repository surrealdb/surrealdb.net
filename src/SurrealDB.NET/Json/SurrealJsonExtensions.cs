using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDB.NET.Json;

public static class SurrealJsonExtensions
{
	public static void WriteThingValue([NotNull] this Utf8JsonWriter writer, in Thing thing)
	{
		writer.WriteStringValue(string.IsNullOrWhiteSpace(thing.Id)
			? thing.Table.Name
			: $"{thing.Table.Name}:{thing.Id}");
	}

	public static void WriteRecordValueWithoutId<T>([NotNull] this Utf8JsonWriter writer, T record, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		writer.WriteStartObject();

		var element = JsonSerializer.SerializeToElement(record, options);

		foreach (var p in element.EnumerateObject().Where(p => !p.NameEquals("id"u8)))
		{
			if (p.Value.ValueKind is JsonValueKind.Null && options.DefaultIgnoreCondition is JsonIgnoreCondition.WhenWritingNull or JsonIgnoreCondition.WhenWritingDefault)
				continue;

			p.WriteTo(writer);
		}

		writer.WriteEndObject();
	}
}
