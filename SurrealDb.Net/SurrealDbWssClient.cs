using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Helpers;

namespace SurrealDb.Net;

public static class SurrealDbWssClient
{
	/// <summary>
	/// Creates a new SurrealDbClient using the WSS protocol.
	/// </summary>
	/// <param name="host">The host name of the SurrealDB instance.</param>
	/// <param name="ns">The table namespace to connect to.</param>
	/// <param name="db">The table database to connect to.</param>
	/// <param name="username">The username to connect to (with root access).</param>
	/// <param name="password">The password to connect to (with root access).</param>
	/// <param name="token">The token to connect to (with user access).</param>
	/// <exception cref="ArgumentException">Thrown when host is not valid.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when port in host is provided but is value is out of range.</exception>
	public static ISurrealDbClient New(string host, string? ns = null, string? db = null, string? username = null, string? password = null, string? token = null)
	{
#if NET6_0_OR_GREATER
		string endpoint = UriBuilderHelper.CreateEndpointFromProtocolAndHost(host, Uri.UriSchemeWss, "/rpc");
#else
		const string protocol = "wss";
		string endpoint = UriBuilderHelper.CreateEndpointFromProtocolAndHost(host, protocol, "/rpc");
#endif

		var options = new SurrealDbOptions
		{
			Endpoint = endpoint,
			Namespace = ns,
			Database = db,
			Username = username,
			Password = password,
			Token = token
		};
		return new SurrealDbClient(options, null);
	}
}
