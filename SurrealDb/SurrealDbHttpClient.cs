using SurrealDb.Internals.Helpers;

namespace SurrealDb;

public static class SurrealDbHttpClient
{
    /// <summary>
    /// Creates a new SurrealDbClient using the HTTP protocol.
    /// </summary>
    /// <param name="host">The host name of the SurrealDB instance.</param>
    /// <param name="httpClientFactory">An IHttpClientFactory instance, or none.</param>
    /// <exception cref="ArgumentException"></exception>
    public static ISurrealDbClient New(string host, IHttpClientFactory? httpClientFactory = null)
	{
		const string protocol = "http";
		string endpoint = UriBuilderHelper.CreateEndpointFromProtocolAndHost(host, protocol);

		return new SurrealDbClient(endpoint, null, null, null, null, httpClientFactory);
	}
}
