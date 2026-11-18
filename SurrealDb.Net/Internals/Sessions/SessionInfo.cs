using System.Collections.Concurrent;
using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Cbor;

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

    public void Use(string? ns, string? db)
    {
        Ns = ns;
        Db = db;
    }

    public void UseNs(string? ns)
    {
        Ns = ns;

        // 💡 As on the server, a database cannot stay selected without a namespace
        if (ns is null)
        {
            Db = null;
        }
    }

    public void UseDb(string? db)
    {
        Db = db;
    }

    /// <summary>
    /// Clears the selected namespace and database, and the variables.
    /// </summary>
    public virtual void Reset()
    {
        Ns = null;
        Db = null;
        _variables.Clear();
    }

    /// <summary>
    /// Keeps a variable, encoded as it is now: the copy sent again later (with each HTTP request,
    /// after a reconnection, or to a forked session) must not see later changes to the object.
    /// </summary>
    public void Set(string key, object? value, CborOptions options)
    {
        _variables[key] = EncodedCborValue.Create(value, options);
    }

    public void Unset(string key)
    {
        _variables.TryRemove(key, out _);
    }
}
