using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Ws;

namespace SurrealDb.Net.Internals.Json.Converters;

internal class SurrealDbWsResponseConverter : JsonConverter<ISurrealDbWsResponse>
{
    public override ISurrealDbWsResponse? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (
            !root.TryGetProperty(
                SurrealDbWsResponseConstants.IdPropertyName,
                out var rootIdProperty
            )
        )
        {
            if (
                root.TryGetProperty(
                    SurrealDbWsResponseConstants.ResultPropertyName,
                    out var liveResultElement
                )
            )
            {
                if (
                    liveResultElement.TryGetProperty(
                        SurrealDbWsResponseConstants.IdPropertyName,
                        out var liveIdProperty
                    )
                    && liveResultElement.TryGetProperty(
                        SurrealDbWsResponseConstants.ActionPropertyName,
                        out var liveActionProperty
                    )
                    && liveResultElement.TryGetProperty(
                        SurrealDbWsResponseConstants.ResultPropertyName,
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

        if (
            root.TryGetProperty(
                SurrealDbWsResponseConstants.ResultPropertyName,
                out var resultProperty
            )
        )
        {
            string id = rootIdProperty.GetString() ?? string.Empty;

            return new SurrealDbWsOkResponse(id, resultProperty.Clone(), options);
        }

        if (
            root.TryGetProperty(
                SurrealDbWsResponseConstants.ErrorPropertyName,
                out var errorProperty
            )
        )
        {
            string id = rootIdProperty.GetString() ?? string.Empty;

            long code = errorProperty.TryGetProperty(
                SurrealDbWsResponseConstants.CodePropertyName,
                out var codeProperty
            )
                ? codeProperty.GetInt64()
                : default;
            var message = errorProperty.TryGetProperty(
                SurrealDbWsResponseConstants.MessagePropertyName,
                out var messageProperty
            )
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
            $"Cannot write {nameof(ISurrealDbWsResponse)} back in json..."
        );
    }
}
