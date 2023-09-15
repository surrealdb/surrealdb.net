using System.Text.Json;

namespace SurrealDB.NET.Json;

public static class SurrealJson
{
    public static JsonElement BytesToJsonElement(ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json);
        return JsonElement.ParseValue(ref reader);
    }
}
