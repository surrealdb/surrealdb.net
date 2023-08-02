using System.Text.Json.Serialization;

namespace SurrealDb.Models.Auth;

/// <summary>
/// Credentials for the namespace user
/// </summary>
public sealed class NamespaceAuth
{
	/// <summary>
	/// The namespace the user has access to
	/// </summary>
	[JsonPropertyName("ns")]
	public string Namespace { get; set; } = string.Empty;

	/// <summary>
	/// The username of the namespace user
	/// </summary>
	[JsonPropertyName("user")]
	public string Username { get; set; } = string.Empty;

	/// <summary>
	/// The password of the namespace user
	/// </summary>
	[JsonPropertyName("pass")]
	public string Password { get; set; } = string.Empty;
}
