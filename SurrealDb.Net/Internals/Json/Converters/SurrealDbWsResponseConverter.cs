using SurrealDb.Net.Internals.Ws;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Json.Converters;

internal class SurrealDbWsResponseConverter : JsonConverter<ISurrealDbWsResponse>
{
	const string ResultPropertyName = "result";
	const string ErrorPropertyName = "error";

	public override ISurrealDbWsResponse? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using var doc = JsonDocument.ParseValue(ref reader);
		var root = doc.RootElement;

		if (root.TryGetProperty(ResultPropertyName, out _))
			return JsonSerializer.Deserialize<SurrealDbWsOkResponse>(root.GetRawText(), options);

		if (root.TryGetProperty(ErrorPropertyName, out _))
			return JsonSerializer.Deserialize<SurrealDbWsErrorResponse>(root.GetRawText(), options);

		return new SurrealDbWsUnknownResponse();
	}

	public override void Write(Utf8JsonWriter writer, ISurrealDbWsResponse value, JsonSerializerOptions options)
	{
		JsonSerializer.Serialize(writer, value, options);
	}
}
