using System.Collections.Concurrent;
using SurrealDb.Net.Internals.Sessions;

namespace SurrealDb.Net.Internals.DependencyInjection;

internal sealed class Sessionizer : ISessionizer
{
    private readonly ConcurrentDictionary<Guid, ISessionInfo> _sessionInfos = new();

    public void Add(Guid sessionId, ISessionInfo sessionInfo)
    {
        _sessionInfos.TryAdd(sessionId, sessionInfo);
    }

    public bool Get(Guid sessionId, out ISessionInfo? sessionInfo)
    {
        return _sessionInfos.TryGetValue(sessionId, out sessionInfo);
    }

    public void TryRemove(Guid sessionId)
    {
        _sessionInfos.Remove(sessionId, out _);
    }
}
