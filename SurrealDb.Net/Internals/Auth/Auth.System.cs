using SurrealDb.Net.Models.Auth;

namespace SurrealDb.Net.Internals.Auth;

internal sealed record InternalSystemAuth(SystemAuth Auth) : IAuth;
