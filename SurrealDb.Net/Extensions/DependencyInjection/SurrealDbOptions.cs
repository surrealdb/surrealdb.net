using SurrealDb.Net;

namespace Microsoft.Extensions.DependencyInjection;

public class SurrealDbOptions
{
	/// <summary>
	/// Endpoint of the SurrealDB instance.<br /><br />
	/// Examples:<br />
	/// - http://localhost:8000<br />
	/// - wss://cloud.surrealdb.com
	/// </summary>
	public string? Endpoint { get; internal set; }

	/// <summary>
	/// Default namespace to use when new <see cref="ISurrealDbClient"/> is generated.
	/// </summary>
	public string? Namespace { get; internal set; }

	/// <summary>
	/// Default database to use when new <see cref="ISurrealDbClient"/> is generated.
	/// </summary>
	public string? Database { get; internal set; }

	/// <summary>
	/// Default username (Root auth) to use when new <see cref="ISurrealDbClient"/> is generated.
	/// </summary>
	public string? Username { get; internal set; }

	/// <summary>
	/// Default password (Root auth) to use when new <see cref="ISurrealDbClient"/> is generated.
	/// </summary>
	public string? Password { get; internal set; }

	/// <summary>
	/// Default token (User auth) to use when new <see cref="ISurrealDbClient"/> is generated.
	/// </summary>
	public string? Token { get; internal set; }

	public static SurrealDbOptionsBuilder Create()
	{
		return new SurrealDbOptionsBuilder();
	}
}
