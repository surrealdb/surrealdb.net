using SurrealDB.NET.Json.Converters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDB.NET;

public sealed class SurrealOptions
{
    public required Uri Endpoint { get; set; }

    public string? DefaultNamespace { get; set; }

    public string? DefaultDatabase { get; set; }

	/// <summary>
	/// Allows dropping entire tables. The default value is <see langword="false"/>.
	/// </summary>
	public bool AllowDeleteOnFullTable { get; set; }

	public SurrealOptions()
	{
		var thingConverter = new SurrealThingJsonConverter();
		JsonRequestOptions.Converters.Add(thingConverter);
		JsonResponseOptions.Converters.Add(thingConverter);

		var tableConverter = new SurrealTableJsonConverter();
		JsonRequestOptions.Converters.Add(tableConverter);
		JsonResponseOptions.Converters.Add(tableConverter);

		var timeSpanConverter = new SurrealTimeSpanJsonConverter();
		JsonRequestOptions.Converters.Add(timeSpanConverter);
		JsonResponseOptions.Converters.Add(timeSpanConverter);
	}

	/// <summary>
	/// Options for how SurrealDB.NET serializes requests.
	/// </summary>
	public JsonSerializerOptions JsonRequestOptions { get; } = new()
    { 
        WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNameCaseInsensitive = true,
	};

	/// <summary>
	/// Options for how SurrealDB.NET deserializes responses.
	/// </summary>
	public JsonSerializerOptions JsonResponseOptions { get; } = new()
	{
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNameCaseInsensitive = true,
	};

	public int BufferSize { get; set; } = 4096;
}
