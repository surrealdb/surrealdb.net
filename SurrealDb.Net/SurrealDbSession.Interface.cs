using SurrealDb.Net.Models.Sessions;

namespace SurrealDb.Net;

public interface ISurrealDbSharedSession
{
    /// <summary>
    /// Returns the unique session id. <c>null</c> is this is the default session.
    /// </summary>
    Guid? SessionId { get; }

    /// <summary>
    /// Returns the current session state.
    /// </summary>
    SessionState SessionState { get; }

    /// <summary>
    /// List all active sessions on the current connection.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>A list of all session ids.</returns>
    Task<IEnumerable<Guid>> Sessions(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new isolated session on the current connection.
    /// The new session will have its own namespace, database, variables, and authentication state, but will share the same connection.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>Returns a clean new session.</returns>
    Task<ISurrealDbSession> CreateSession(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the current session and disposes of it. After this method is called, the session cannot be used again.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    Task CloseSession(CancellationToken cancellationToken = default);
}

/// <summary>
/// Session support for <see cref="SurrealDbClient"/>.
/// Multi-sessions feature is only available since SurrealDB v3.0.
/// </summary>
public interface ISurrealDbSession
    : ISurrealDbSharedSession,
        ISurrealDbSharedMethods,
        IAsyncDisposable
{
    /// <summary>
    /// Create a new transaction scoped to the current session.
    /// Transactions allow you to execute multiple queries atomically.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>A new <see cref="SurrealDbTransaction"/>.</returns>
    Task<SurrealDbTransaction> BeginTransaction(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new session by cloning the current session.
    /// The new session inherits all properties from the parent session including namespace, database, variables, and authentication state.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>Returns a new session derived from the current one.</returns>
    Task<ISurrealDbSession> ForkSession(CancellationToken cancellationToken = default);
}
