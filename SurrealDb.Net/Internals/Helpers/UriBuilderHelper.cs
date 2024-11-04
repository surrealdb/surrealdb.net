namespace SurrealDb.Net.Internals.Helpers;

internal static class UriBuilderHelper
{
    public static string CreateEndpointFromProtocolAndHost(
        string host,
        string protocol,
        string? path = null
    )
    {
        string[] parts = host.Split(':');

        if (parts.Length == 2)
        {
            string hostName = parts[0];
            int port = int.Parse(parts[1]);

            if (path is not null)
                return new UriBuilder(protocol, hostName, port, path).ToString();

            return new UriBuilder(protocol, hostName, port).ToString();
        }

        if (parts.Length == 1)
        {
            string hostName = parts[0];

            if (path is not null)
            {
                const int defaultPort = -1;
                return new UriBuilder(protocol, hostName, defaultPort, path).ToString();
            }

            return new UriBuilder(protocol, hostName).ToString();
        }

        throw new ArgumentException("Invalid host name.", nameof(host));
    }
}
