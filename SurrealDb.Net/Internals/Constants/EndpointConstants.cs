namespace SurrealDb.Net.Internals.Constants;

internal static class EndpointConstants
{
    internal static class Client
    {
        public const string MEMORY = "mem://";
        public const string ROCKSDB = "rocksdb://";
        public const string SURREALKV = "surrealkv://";
    }

    internal static class Server
    {
        public const string HTTP = "http://";
        public const string HTTPS = "https://";
        public const string WS = "ws://";
        public const string WSS = "wss://";
    }
}
