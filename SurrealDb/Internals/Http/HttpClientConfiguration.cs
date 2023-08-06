using SurrealDb.Internals.Auth;

namespace SurrealDb.Internals.Http;

internal record HttpClientConfiguration(string? Namespace, string? Database, IAuth Auth);
