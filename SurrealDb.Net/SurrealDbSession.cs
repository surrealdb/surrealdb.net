using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Internals;

namespace SurrealDb.Net;

/// <summary>
/// A session object to communicate with a SurrealDB instance.
/// </summary>
public class SurrealDbSession : BaseSurrealDbClient, ISurrealDbSession
{
    internal SurrealDbSession(BaseSurrealDbClient from, Guid sessionId, Guid? transactionId)
    {
        Uri = from.Uri;
        Engine = from.Engine;
        SessionId = sessionId;
        TransactionId = transactionId;
    }

    public async Task<SurrealDbTransaction> BeginTransaction(
        CancellationToken cancellationToken = default
    )
    {
        if (!SessionId.HasValue)
        {
            throw new InvalidSessionException(SessionId);
        }
        if (!(await SupportsTransactions(cancellationToken).ConfigureAwait(false)))
        {
            throw new NotSupportedException("Transactions are not supported.");
        }

        var transactionId = await Engine.Begin(SessionId, cancellationToken).ConfigureAwait(false);
        return new SurrealDbTransaction(this, SessionId!.Value, transactionId);
    }

    public async Task<ISurrealDbSession> ForkSession(CancellationToken cancellationToken = default)
    {
        if (!SessionId.HasValue)
        {
            throw new InvalidSessionException(SessionId);
        }

        var newId = await Engine
            .CreateSession(SessionId.Value, cancellationToken)
            .ConfigureAwait(false);
        return new SurrealDbSession(this, newId, null);
    }
}
