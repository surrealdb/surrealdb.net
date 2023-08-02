using SurrealDb;

namespace Microsoft.Extensions.DependencyInjection;

public class SurrealDbOptions
{
	/// <summary>
	/// Address to the SurrealDB instance.<br /><br />
	/// Examples:<br />
	/// - http://localhost:8000<br />
	/// - wss://cloud.surrealdb.com
	/// </summary>
	public string? Address { get; internal set; }

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

	public static SurrealDbOptionsBuilder Create()
	{
		return new SurrealDbOptionsBuilder();
	}
}
