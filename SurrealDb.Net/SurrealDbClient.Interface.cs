namespace SurrealDb.Net;

/// <summary>
/// The entry point to communicate with a SurrealDB instance.
/// Authenticate, use namespace/database, execute queries, etc...
/// </summary>
public interface ISurrealDbClient
    : ISurrealDbSharedSession,
        ISurrealDbSharedMethods,
        IAsyncDisposable;
