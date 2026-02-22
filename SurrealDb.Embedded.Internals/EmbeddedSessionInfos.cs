using System.Collections.Concurrent;

namespace SurrealDb.Embedded.Internals;

internal sealed class EmbeddedSessionInfos
{
    private EmbeddedSessionInfo? _rootInfo;
    private readonly ConcurrentDictionary<Guid, EmbeddedSessionInfo> _infos = new();

    public EmbeddedSessionInfo? Get(Guid? id)
    {
        return id.HasValue ? _infos.GetValueOrDefault(id.Value) : _rootInfo;
    }

    public void Set(Guid? id, EmbeddedSessionInfo value)
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

    public EmbeddedSessionInfo Clone(Guid from, Guid newId)
    {
        var cloned = new EmbeddedSessionInfo(Get(from)!);
        Set(newId, cloned);

        return cloned;
    }
}
