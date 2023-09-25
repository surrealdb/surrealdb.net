using SurrealDb.Net.Internals.Auth;

namespace SurrealDb.Net.Internals.Http;

internal record HttpClientConfiguration(string? Namespace, string? Database, IAuth Auth);
