using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace SurrealDb.Net.Internals.Sessions;

internal abstract class SessionInfo : ISessionInfo
{
    public string? Ns { get; private set; }
    public string? Db { get; private set; }

    // 💡 Concurrent, since requests of a session can set and unset variables at the same time
    private readonly ConcurrentDictionary<string, object?> _variables = new();
    public IReadOnlyDictionary<string, object?> Variables => _variables;

    protected SessionInfo() { }

    protected SessionInfo(SessionInfo from)
    {
        Ns = from.Ns;
        Db = from.Db;

        foreach (var (key, value) in from.Variables)
        {
            _variables[key] = value;
        }
    }

    protected SessionInfo(SurrealDbOptions options)
    {
        Ns = options.Namespace;
        Db = options.Database;
    }

    public void Use(string ns, string? db)
    {
        Ns = ns;
        Db = db;
    }

    public void Set(string key, object? value)
    {
        _variables[key] = value;
    }

    public void Unset(string key)
    {
        _variables.TryRemove(key, out _);
    }
}
