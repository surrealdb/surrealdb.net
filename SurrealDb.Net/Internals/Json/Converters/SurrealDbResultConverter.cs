using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Internals.Json.Converters;

internal class SurrealDbResultConverter : JsonConverter<ISurrealDbResult>
{
    public override ISurrealDbResult Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (
            root.TryGetProperty(SurrealDbResultConstants.StatusPropertyName, out var statusProperty)
        )
        {
            var status = statusProperty.GetString();

            var timeProperty = root.GetProperty(SurrealDbResultConstants.TimePropertyName);
            var time = timeProperty.ValueKind switch
            {
                JsonValueKind.Undefined or JsonValueKind.Null => TimeSpan.Zero,
                JsonValueKind.String
                    => TimeSpanValueConverter.GetValueFromString(timeProperty.GetString()),
                JsonValueKind.Number
                    => TimeSpanValueConverter.GetValueFromNumber(timeProperty.GetInt64()),
                _ => throw new JsonException($"Cannot deserialize 'time' to {nameof(TimeSpan)}")
            };

            if (status == SurrealDbResultConstants.OkStatus)
            {
                var value = root.GetProperty(SurrealDbResultConstants.ResultPropertyName).Clone();

                return new SurrealDbOkResult(time, status, value, options);
            }

            if (status is not null)
            {
                var details = root.TryGetProperty(
                    SurrealDbResultConstants.ErrorDetailsPropertyName,
                    out var errorDetailsProperty
                )
                    ? errorDetailsProperty.GetString()
                    : null;

                return new SurrealDbErrorResult(time, status, details!);
            }
        }

        if (
            root.TryGetProperty(SurrealDbResultConstants.CodePropertyName, out var codeProperty)
            && Enum.TryParse(codeProperty.GetInt16().ToString(), true, out HttpStatusCode code)
        )
        {
            // TODO : Use Source Generator to convert a number to HttpStatusCode, instead of ".GetInt16().ToString()"
            var details = root.TryGetProperty(
                SurrealDbResultConstants.DetailsPropertyName,
                out var detailsProperty
            )
                ? detailsProperty.GetString()
                : null;
            var description = root.TryGetProperty(
                SurrealDbResultConstants.DescriptionPropertyName,
                out var descriptionProperty
            )
                ? descriptionProperty.GetString()
                : null;
            var information = root.TryGetProperty(
                SurrealDbResultConstants.InformationPropertyName,
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
