using SurrealDb.Net.Internals.Auth;

namespace SurrealDb.Net.Internals.Http;

internal sealed record HttpClientConfiguration(string? Namespace, string? Database, IAuth Auth);
