using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Models.Sessions;

namespace SurrealDb.Net;

public abstract partial class BaseSurrealDbClient
{
#if NET9_0_OR_GREATER
    private readonly Lock _sessionLock = new();
#else
    private readonly object _sessionLock = new();
#endif

    internal bool IsRootSession => !SessionId.HasValue;

    public Guid? SessionId { get; protected init; }
    public SessionState SessionState { get; private set; }

    public Task<IEnumerable<Guid>> Sessions(CancellationToken cancellationToken = default)
    {
        return Engine.Sessions(cancellationToken);
    }

    public async Task<ISurrealDbSession> CreateSession(
        CancellationToken cancellationToken = default
    )
    {
        var newId = await Engine.CreateSession(cancellationToken).ConfigureAwait(false);
        return new SurrealDbSession(this, newId);
    }

    public Task CloseSession(CancellationToken cancellationToken = default)
    {
        if (SessionId.HasValue)
        {
            bool shouldClose = false;

            lock (_sessionLock)
            {
                if (SessionState != SessionState.Closed)
                {
                    SessionState = SessionState.Closed;
                    shouldClose = true;
                }
            }

            if (shouldClose)
            {
                return Engine.CloseSession(SessionId.Value, cancellationToken);
            }

            return Task.CompletedTask;
        }

        throw new InvalidSessionException(SessionId);
    }
}
