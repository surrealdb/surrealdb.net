using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.DependencyInjection;
using SurrealDb.Net.Internals.Sessions;

namespace SurrealDb.Embedded.Internals;

internal sealed class EmbeddedSessionInfoProvider : ISessionInfoProvider
{
    private static readonly ConcurrentDictionary<SurrealDbOptions, ISessionInfo> _cache = new();

    public ISessionInfo Get(SurrealDbOptions options)
    {
        return _cache.GetOrAdd(options, o => new EmbeddedSessionInfo(o));
    }
}
