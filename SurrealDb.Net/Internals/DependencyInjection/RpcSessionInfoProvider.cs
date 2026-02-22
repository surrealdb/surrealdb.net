using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Sessions;

namespace SurrealDb.Net.Internals.DependencyInjection;

internal sealed class RpcSessionInfoProvider : ISessionInfoProvider
{
    private static readonly ConcurrentDictionary<SurrealDbOptions, ISessionInfo> _cache = new();

    public ISessionInfo Get(SurrealDbOptions options)
    {
        return _cache.GetOrAdd(options, o => new RpcSessionInfo(o));
    }
}
