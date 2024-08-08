using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Http;

namespace SurrealDb.Net.Internals.Json.Converters;

internal class SurrealDbHttpResponseConverter : JsonConverter<ISurrealDbHttpResponse>
{
    public override ISurrealDbHttpResponse? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (
            root.TryGetProperty(SurrealDbResponseConstants.ErrorPropertyName, out var errorProperty)
        )
        {
            long code = errorProperty.TryGetProperty(
                SurrealDbResponseConstants.CodePropertyName,
                out var codeProperty
            )
                ? codeProperty.GetInt64()
                : default;
            var message = errorProperty.TryGetProperty(
                SurrealDbResponseConstants.MessagePropertyName,
                out var messageProperty
            )
                ? messageProperty.GetString()
                : default;

            var content = new SurrealDbHttpErrorResponseContent { Code = code, Message = message! };
            return new SurrealDbHttpErrorResponse { Error = content };
        }

        if (
            root.TryGetProperty(
                SurrealDbResponseConstants.ResultPropertyName,
                out var resultProperty
            )
        )
        {
            return new SurrealDbHttpOkResponse(resultProperty.Clone(), options);
        }

        return new SurrealDbHttpUnknownResponse();
    }

    public override void Write(
        Utf8JsonWriter writer,
        ISurrealDbHttpResponse value,
        JsonSerializerOptions options
    )
    {
        throw new NotSupportedException(
            $"Cannot write {nameof(ISurrealDbHttpResponse)} back in json..."
        );
    }
}
