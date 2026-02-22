using SurrealDb.Net.Internals.Sessions;

namespace SurrealDb.Net.Internals.DependencyInjection;

public interface ISessionizer
{
    void Add(Guid sessionId, ISessionInfo sessionInfo);
    bool Get(Guid sessionId, out ISessionInfo? sessionInfo);
    void TryRemove(Guid sessionId);
}
