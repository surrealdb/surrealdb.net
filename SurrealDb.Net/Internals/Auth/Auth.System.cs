using SurrealDb.Net.Models.Auth;

namespace SurrealDb.Net.Internals.Auth;

internal record InternalSystemAuth(SystemAuth Auth) : IAuth;
