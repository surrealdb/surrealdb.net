using System.Text.Json.Serialization;

namespace SurrealDb.Models.Auth;

/// <summary>
/// Credentials for the root user
/// </summary>
public sealed class RootAuth
{
	/// <summary>
	/// The username of the root user
	/// </summary>
	[JsonPropertyName("user")]
    public string Username { get; set; } = string.Empty;

	/// <summary>
	/// The password of the root user
	/// </summary>
	[JsonPropertyName("pass")]
    public string Password { get; set; } = string.Empty;
}
