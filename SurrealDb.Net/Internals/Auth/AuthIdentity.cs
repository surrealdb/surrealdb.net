using SurrealDb.Net.Internals.Helpers;
using SurrealDb.Net.Models.Auth;

namespace SurrealDb.Net.Internals.Auth;

internal static class AuthIdentity
{
    /// <summary>
    /// Who a session is signed in as, to tell whether it is still the same user: <c>null</c> when it is not signed in.
    /// A renewed token is the same user, since it keeps the namespace, database, access method and record of the first one.
    /// </summary>
    public static string? Of(IAuth? auth)
    {
        return auth switch
        {
            InternalSystemAuth { Auth: NamespaceAuth namespaceAuth } =>
                $"namespace|{namespaceAuth.Namespace}|{namespaceAuth.Username}",
            InternalSystemAuth { Auth: DatabaseAuth databaseAuth } =>
                $"database|{databaseAuth.Namespace}|{databaseAuth.Database}|{databaseAuth.Username}",
            InternalSystemAuth { Auth: var systemAuth } => $"root|{systemAuth.Username}",
            BearerAuth bearerAuth => JwtHelper.TryGetSubject(bearerAuth.Token, out var subject)
                ? $"token|{subject}"
                : $"token|{bearerAuth.Token}",
            _ => null,
        };
    }
}
