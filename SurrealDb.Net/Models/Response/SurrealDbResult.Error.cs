using System.Text.Json.Serialization;

namespace SurrealDb.Net.Models.Response;

/// <summary>
/// A SurrealDB error result that can be returned from a query request.
/// </summary>
public sealed class SurrealDbErrorResult : ISurrealDbErrorResult
{
	/// <summary>
	/// Time taken to execute the query.
	/// </summary>
	[JsonPropertyName("time")]
	public TimeSpan Time { get; set; }

	/// <summary>
	/// Status of the query ("ERR").
	/// </summary>
	[JsonPropertyName("status")]
	public string Status { get; set; } = string.Empty;

	/// <summary>
	/// Details about the error.
	/// </summary>
	[JsonPropertyName("detail")]
	public string Details { get; set; } = string.Empty;

	public bool IsOk => false;
}
