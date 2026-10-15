namespace SurrealDb.Net.Models.Api;

/// <summary>
/// A request sent to an API defined with <c>DEFINE API</c>.
/// </summary>
public sealed class SurrealDbApiRequest
{
    /// <summary>
    /// The HTTP method, e.g. <c>get</c> or <c>post</c>. Defaults to <c>get</c>.
    /// </summary>
    public string Method { get; set; } = "get";

    /// <summary>
    /// The body of the request, if any.
    /// </summary>
    public object? Body { get; set; }

    /// <summary>
    /// The headers of the request.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// The query string parameters of the request.
    /// </summary>
    public IDictionary<string, string> Query { get; set; } = new Dictionary<string, string>();
}
