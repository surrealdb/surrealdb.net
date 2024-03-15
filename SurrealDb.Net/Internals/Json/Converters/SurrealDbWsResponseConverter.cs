using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Internals.Ws;

namespace SurrealDb.Net.Internals.Json.Converters;

internal class SurrealDbWsResponseConverter : JsonConverter<ISurrealDbWsResponse>
{
    const string IdPropertyName = "id";
    const string ResultPropertyName = "result";
    const string ErrorPropertyName = "error";
    const string ActionPropertyName = "action";
    const string CodePropertyName = "code";
    const string MessagePropertyName = "message";

    public override ISurrealDbWsResponse? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty(IdPropertyName, out var rootIdProperty))
        {
            if (root.TryGetProperty(ResultPropertyName, out var liveResultElement))
            {
                if (
                    liveResultElement.TryGetProperty(IdPropertyName, out var liveIdProperty)
                    && liveResultElement.TryGetProperty(
                        ActionPropertyName,
                        out var liveActionProperty
                    )
                    && liveResultElement.TryGetProperty(
                        ResultPropertyName,
                        out var liveResultProperty
                    )
                )
                {
                    var id = liveIdProperty.GetGuid();
                    var action = liveActionProperty.GetString();
                    var result = liveResultProperty.Clone();

                    var content = new SurrealDbWsLiveResponseContent(id, action!, result, options);
                    return new SurrealDbWsLiveResponse(content);
                }
            }

            return new SurrealDbWsUnknownResponse();
        }

        if (root.TryGetProperty(ResultPropertyName, out var resultProperty))
        {
            string id = rootIdProperty.GetString() ?? string.Empty;

            return new SurrealDbWsOkResponse(id, resultProperty.Clone(), options);
        }

        if (root.TryGetProperty(ErrorPropertyName, out var errorProperty))
        {
            string id = rootIdProperty.GetString() ?? string.Empty;

            long code = errorProperty.TryGetProperty(CodePropertyName, out var codeProperty)
                ? codeProperty.GetInt64()
                : default;
            var message = errorProperty.TryGetProperty(MessagePropertyName, out var messageProperty)
                ? messageProperty.GetString()
                : default;

            var content = new SurrealDbWsErrorResponseContent { Code = code, Message = message! };
            return new SurrealDbWsErrorResponse { Id = id, Error = content };
        }

        return new SurrealDbWsUnknownResponse();
    }

    public override void Write(
        Utf8JsonWriter writer,
        ISurrealDbWsResponse value,
        JsonSerializerOptions options
    )
    {
        throw new NotSupportedException(
            $"Cannot write {nameof(SurrealDbWsResponseConverter)} back in json..."
        );
    }
}
