using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDB.NET.Json.Converters;

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
        var result = _root.ValueKind is JsonValueKind.Array
			? _root[page]
			: _root.GetProperty("result")[page];

        var set = result.Deserialize<SurrealQueryResultPage>();
        
        if (set.Status is "OK")
		{
			if (set.Result.ValueKind is JsonValueKind.Array && !typeof(System.Collections.IEnumerable).IsAssignableFrom(typeof(T)))
			{
				var length = set.Result.GetArrayLength();

				if (length is 0)
					return default;

				return set.Result[0].Deserialize<T>();
			}

			return set.Result.Deserialize<T>();
		}

        throw new SurrealException($"Surreal query result was an error: {set.Result.Deserialize<string>()}");
    }
}

internal readonly record struct SurrealQueryResultPage
{
	[JsonPropertyName("result")]
	public JsonElement Result { get; init; }

	[JsonPropertyName("status")]
	public string Status { get; init; }

	[JsonPropertyName("time"), JsonConverter(typeof(SurrealTimeSpanJsonConverter))]
	public TimeSpan Time { get; init; }
}
