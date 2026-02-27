using System.Collections.Concurrent;

namespace SurrealDb.Net.Internals.Sessions;

internal sealed class RpcSessionInfos
{
    private RpcSessionInfo? _rootInfo;
    private readonly ConcurrentDictionary<Guid, RpcSessionInfo> _infos = new();

    public RpcSessionInfo? Get(Guid? id)
    {
        return id.HasValue ? _infos.GetValueOrDefault(id.Value) : _rootInfo;
    }

    public void Set(Guid? id, RpcSessionInfo value)
    {
        if (id.HasValue)
        {
            _infos.AddOrUpdate(id.Value, value, (_, _) => value);
            return;
        }

        _rootInfo = value;
    }

    public void Remove(Guid sessionId)
    {
        _infos.Remove(sessionId, out _);
    }

    public RpcSessionInfo Clone(Guid from, Guid newId)
    {
        var cloned = new RpcSessionInfo(Get(from)!);
        Set(newId, cloned);

        return cloned;
    }

    public IEnumerable<Guid?> Enumerate()
    {
        var childSessionIds = _infos.Keys;
        foreach (var childSessionId in childSessionIds)
        {
            yield return childSessionId;
        }

        if (_rootInfo is not null)
        {
            yield return null;
        }
    }
}
