using System.Text.Json;
using System.Text.Json.Serialization;

using SurrealDB.NET.Json;

namespace SurrealDB.NET;

public readonly record struct SurrealQueryResult
{
    private readonly JsonElement _root;

    internal SurrealQueryResult(JsonElement root)
    {
        _root = root;
    }

    public T? Get<T>(int page = 0)
    {
        var result = _root.GetProperty("result").EnumerateArray().ElementAt(page);
        var set = result.Deserialize<SurrealQueryResultPage>();
        
        if (set.Status is "OK")
            return set.Result.Deserialize<T>();

        throw new InvalidOperationException($"Surreal query result was an error: {set.Result.Deserialize<string>()}");
    }

    private readonly record struct SurrealQueryResultPage
    {
        [JsonPropertyName("result")]
        public JsonElement Result { get; init; }

        [JsonPropertyName("status")]
        public string Status { get; init; }

        [JsonPropertyName("time"), JsonConverter(typeof(SurrealTimeSpanJsonConverter))]
        public TimeSpan Time { get; init; }
    }
}

#if DEBUG
internal static class DebugHelpers
{
    public static string ToJson(this object? obj, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(obj, options ?? new JsonSerializerOptions { WriteIndented = true });
    }

    public static string ToJson(this IEnumerable<byte>? obj, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonDocument>(obj.ToArray()), options ?? new JsonSerializerOptions { WriteIndented = true });
    }
}
#endif