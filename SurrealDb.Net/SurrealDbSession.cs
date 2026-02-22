using SurrealDb.Net.Exceptions;

namespace SurrealDb.Net;

/// <summary>
/// A session object to communicate with a SurrealDB instance.
/// </summary>
public class SurrealDbSession : BaseSurrealDbClient, ISurrealDbSession
{
    internal SurrealDbSession(BaseSurrealDbClient from, Guid sessionId)
    {
        Uri = from.Uri;
        Engine = from.Engine;
        SessionId = sessionId;
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
        return new SurrealDbSession(this, newId);
    }
}
