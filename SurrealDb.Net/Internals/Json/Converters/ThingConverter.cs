using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Json.Converters;

public sealed class ThingConverter : JsonConverter<Thing>
{
    public override Thing? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return GetValueFromString(reader.GetString());
    }

    private static Thing? GetValueFromString(string? value)
    {
        if (value is null)
            return default;

        return new Thing(value);
    }

    public override void Write(Utf8JsonWriter writer, Thing value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
