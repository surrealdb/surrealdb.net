using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.AgentMemory.Client;

/// <summary>
/// Reads every timestamp as UTC, whichever offset it was written with.
/// </summary>
/// <remarks>
/// System.Text.Json reads the nanosecond timestamps the service writes, but it hands back a
/// <see cref="DateTime"/> whose <see cref="DateTime.Kind"/> depends on the text: <c>Z</c> gives UTC, a
/// numeric offset such as <c>+00:00</c> gives local time, and no offset gives an unspecified kind.
/// Comparing those with each other ignores the kind, so the same instant would compare differently
/// depending on how it was spelled. A value without an offset is taken to be UTC.
/// </remarks>
internal sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var value = reader.GetDateTime();
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
