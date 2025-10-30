using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Auth;
using SurrealDb.Net.Models.Auth;

namespace SurrealDb.Net.Internals.Ws;

internal sealed class SurrealDbWsEngineConfig
{
    public IAuth Auth { get; private set; } = new NoAuth();
    public string? Ns { get; private set; }
    public string? Db { get; private set; }

    public SurrealDbWsEngineConfig(SurrealDbOptions options)
    {
        Reset(options);
    }

    public void Use(string ns, string? db)
    {
        Ns = ns;
        Db = db;
    }

    public void SetSystemAuth(SystemAuth auth)
    {
        Auth = new InternalSystemAuth(auth);
    }

    private void SetSystemAuth(string username, string? password, SystemAuthLevel systemAuthLevel)
    {
        SystemAuth auth = systemAuthLevel switch
        {
            SystemAuthLevel.Root => new RootAuth { Username = username, Password = password! },
            SystemAuthLevel.Namespace => new NamespaceAuth
            {
                Username = username,
                Password = password!,
                Namespace = Ns!,
            },
            SystemAuthLevel.Database => new DatabaseAuth
            {
                Username = username,
                Password = password!,
                Namespace = Ns!,
                Database = Db!,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(systemAuthLevel)),
        };
        SetSystemAuth(auth);
    }

    public void SetBearerAuth(string token)
    {
        Auth = new BearerAuth(token);
    }

    public void ResetAuth()
    {
        Auth = new NoAuth();
    }

    private void Reset(SurrealDbOptions options)
    {
        Ns = options.Namespace;
        Db = options.Database;
        if (options.Username is not null)
        {
            SetSystemAuth(options.Username, options.Password, options.SystemAuthLevel);
        }
        else if (options.Token is not null)
        {
            SetBearerAuth(options.Token);
        }
        else
        {
            ResetAuth();
        }
    }
}
