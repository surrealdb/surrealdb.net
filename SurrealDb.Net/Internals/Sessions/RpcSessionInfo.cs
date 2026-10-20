using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Auth;
using SurrealDb.Net.Models.Auth;

namespace SurrealDb.Net.Internals.Sessions;

internal sealed class RpcSessionInfo : SessionInfo
{
    public IAuth Auth { get; private set; } = new NoAuth();

    public RpcSessionInfo() { }

    public RpcSessionInfo(RpcSessionInfo from)
        : base(from)
    {
        Auth = from.Auth switch
        {
            InternalSystemAuth internalSystemAuth => internalSystemAuth with { },
            // 💡 A refresh token can be used once: it stays with the original session,
            // otherwise the first copy to renew would revoke it for all the others
            BearerAuth bearerAuth => bearerAuth with { Refresh = null },
            _ => Auth,
        };
    }

    public RpcSessionInfo(SurrealDbOptions options)
        : base(options)
    {
        if (options.Username is not null)
        {
            SetSystemAuth(options.Username, options.Password, options.SystemAuthLevel);
        }
        else if (options.Token is not null)
        {
            SetBearerAuth(options.Token);
        }
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

    public void SetBearerAuth(string token, string? refresh = null)
    {
        Auth = new BearerAuth(token, refresh);
    }

    /// <summary>
    /// Forgets the refresh token, if it is the one given.
    /// </summary>
    public void ForgetRefreshToken(string refresh)
    {
        if (Auth is BearerAuth bearerAuth && bearerAuth.Refresh == refresh)
        {
            Auth = bearerAuth with { Refresh = null };
        }
    }

    public void ResetAuth()
    {
        Auth = new NoAuth();
    }
}
