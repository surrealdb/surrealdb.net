using SurrealDb.Net.Internals.Ws;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Json.Converters;

internal class SurrealDbWsResponseConverter : JsonConverter<ISurrealDbWsResponse>
{
    const string IdPropertyName = "id";
    const string ResultPropertyName = "result";
    const string ErrorPropertyName = "error";
    const string ActionPropertyName = "action";

    public override ISurrealDbWsResponse? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty(IdPropertyName, out var idProperty))
        {
            if (root.TryGetProperty(ResultPropertyName, out var liveResultElement))
            {
                if (
                    liveResultElement.TryGetProperty(IdPropertyName, out _)
                    && liveResultElement.TryGetProperty(ActionPropertyName, out _)
                    && liveResultElement.TryGetProperty(ResultPropertyName, out _)
                )
                {
                    return JsonSerializer.Deserialize<SurrealDbWsLiveResponse>(
                        root.GetRawText(),
                        options
                    );
                }
            }

            return new SurrealDbWsUnknownResponse();
        }

        if (root.TryGetProperty(ResultPropertyName, out var resultProperty))
        {
            string id = idProperty.GetString() ?? string.Empty;

            return new SurrealDbWsOkResponse(id, resultProperty.Clone(), options);
        }

        if (root.TryGetProperty(ErrorPropertyName, out _))
            return JsonSerializer.Deserialize<SurrealDbWsErrorResponse>(root.GetRawText(), options);

        return new SurrealDbWsUnknownResponse();
    }

    public override void Write(
        Utf8JsonWriter writer,
        ISurrealDbWsResponse value,
        JsonSerializerOptions options
    )
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
