using SurrealDb.Internals.Helpers;

namespace SurrealDb;

public static class SurrealDbHttpsClient
{
    /// <summary>
    /// Creates a new SurrealDbClient using the HTTPS protocol.
    /// </summary>
    /// <param name="host">The host name of the SurrealDB instance.</param>
    /// <param name="httpClientFactory">An IHttpClientFactory instance, or none.</param>
    /// <exception cref="ArgumentException"></exception>
    public static ISurrealDbClient New(string host, IHttpClientFactory? httpClientFactory = null)
    {
		string endpoint = UriBuilderHelper.CreateEndpointFromProtocolAndHost(host, Uri.UriSchemeHttps);

		return new SurrealDbClient(endpoint, null, null, null, null, httpClientFactory);
    }
}
