using System.Net;

namespace SurrealDb.Net.Internals.Models;

internal class AuthResponse
{
    public HttpStatusCode Code { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? Token { get; set; }
}
