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

    private object? _body;

    /// <summary>
    /// The body of the request. Leave it unset to send no body (<c>NONE</c>),
    /// or set it to <c>null</c> to send a <c>NULL</c> body.
    /// </summary>
    public object? Body
    {
        get => _body;
        set
        {
            _body = value;
            HasBody = true;
        }
    }

    /// <summary>
    /// Whether <see cref="Body"/> was set, even to <c>null</c>.
    /// </summary>
    public bool HasBody { get; private set; }

    /// <summary>
    /// The headers of the request.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// The query string parameters of the request.
    /// </summary>
    public IDictionary<string, string> Query { get; set; } = new Dictionary<string, string>();
}
