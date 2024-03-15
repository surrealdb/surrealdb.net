using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Internals.Json.Converters;

internal class SurrealDbResultConverter : JsonConverter<ISurrealDbResult>
{
    const string OkStatus = "OK";
    const string StatusPropertyName = "status";
    const string TimePropertyName = "time";
    const string ResultPropertyName = "result";
    const string ErrorDetailsPropertyName = "detail";
    const string CodePropertyName = "code";
    const string DetailsPropertyName = "details";
    const string DescriptionPropertyName = "description";
    const string InformationPropertyName = "information";

    public override ISurrealDbResult Read(
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

            var timeProperty = root.GetProperty(TimePropertyName);
            var time = timeProperty.ValueKind switch
            {
                JsonValueKind.Undefined or JsonValueKind.Null => TimeSpan.Zero,
                JsonValueKind.String
                    => TimeSpanValueConverter.GetValueFromString(timeProperty.GetString()),
                JsonValueKind.Number
                    => TimeSpanValueConverter.GetValueFromNumber(timeProperty.GetInt64()),
                _ => throw new JsonException($"Cannot deserialize 'time' to {nameof(TimeSpan)}")
            };

            if (status == OkStatus)
            {
                var value = root.GetProperty(ResultPropertyName).Clone();

                return new SurrealDbOkResult(time, status, value, options);
            }

            if (status is not null)
            {
                var details = root.TryGetProperty(
                    ErrorDetailsPropertyName,
                    out var errorDetailsProperty
                )
                    ? errorDetailsProperty.GetString()
                    : null;

                return new SurrealDbErrorResult(time, status, details!);
            }
        }

        if (
            root.TryGetProperty(CodePropertyName, out var codeProperty)
            && Enum.TryParse(codeProperty.GetInt16().ToString(), true, out HttpStatusCode code)
        )
        {
            // TODO : Use Source Generator to convert a number to HttpStatusCode, instead of ".GetInt16().ToString()"
            var details = root.TryGetProperty(DetailsPropertyName, out var detailsProperty)
                ? detailsProperty.GetString()
                : null;
            var description = root.TryGetProperty(
                DescriptionPropertyName,
                out var descriptionProperty
            )
                ? descriptionProperty.GetString()
                : null;
            var information = root.TryGetProperty(
                InformationPropertyName,
                out var informationProperty
            )
                ? informationProperty.GetString()
                : null;

            return new SurrealDbProtocolErrorResult(code, details!, description!, information!);
        }

        return new SurrealDbUnknownResult();
    }

    public override void Write(
        Utf8JsonWriter writer,
        ISurrealDbResult value,
        JsonSerializerOptions options
    )
    {
        throw new NotSupportedException($"Cannot write {nameof(ISurrealDbResult)} back in json...");
    }
}
