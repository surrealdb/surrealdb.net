using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDB.NET;

public sealed class SurrealOptions
{
    public const string Section = "SurrealDB";

    public required Uri Endpoint { get; set; }

    public required string DefaultNamespace { get; set; }

    public required string DefaultDatabase { get; set; }

	/// <summary>
	/// Allows dropping entire tables. The default value is <see langword="false"/>.
	/// </summary>
	public required bool AllowDeleteOnFullTable { get; set; }

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
}
