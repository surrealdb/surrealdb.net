using SurrealDb.Net.Models.Response;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Json.Converters;

internal class SurrealDbResultConverter : JsonConverter<ISurrealDbResult>
{
    const string OkStatus = "OK";
    const string StatusPropertyName = "status";
    const string TimePropertyName = "time";
    const string ResultPropertyName = "result";
    const string CodePropertyName = "code";

    public override ISurrealDbResult? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.TryGetProperty(StatusPropertyName, out var statusProperty))
        {
            var status = statusProperty.GetString();

            if (status == OkStatus)
            {
                var timeProperty = root.GetProperty(TimePropertyName);
                var time =
                    timeProperty.ValueKind == JsonValueKind.Null
                        ? TimeSpan.Zero
                        : JsonSerializer.Deserialize<TimeSpan>(timeProperty.GetRawText(), options);

                var value = root.GetProperty(ResultPropertyName).Clone();

                return new SurrealDbOkResult(time, status, value);
            }

            return JsonSerializer.Deserialize<SurrealDbErrorResult>(root.GetRawText(), options);
        }

        if (root.TryGetProperty(CodePropertyName, out _))
            return JsonSerializer.Deserialize<SurrealDbProtocolErrorResult>(
                root.GetRawText(),
                options
            );

        return new SurrealDbUnknownResult();
    }

    public override void Write(
        Utf8JsonWriter writer,
        ISurrealDbResult value,
        JsonSerializerOptions options
    )
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
