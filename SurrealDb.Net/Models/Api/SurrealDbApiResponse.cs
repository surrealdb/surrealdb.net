using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Models.Api;

/// <summary>
/// The response of an API defined with <c>DEFINE API</c>.
/// </summary>
public sealed class SurrealDbApiResponse
{
    private readonly SurrealDbOkResult _body;

    /// <summary>
    /// The path of the API that was invoked.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The HTTP method used to invoke the API.
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// The HTTP status code returned by the API.
    /// </summary>
    public int Status { get; }

    /// <summary>
    /// The headers returned by the API.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    /// Whether <see cref="Status"/> is in the 2xx range.
    /// </summary>
    public bool IsSuccessStatusCode => Status is >= 200 and < 300;

    internal SurrealDbApiResponse(
        string path,
        string method,
        int status,
        IReadOnlyDictionary<string, string> headers,
        SurrealDbOkResult body
    )
    {
        Path = path;
        Method = method;
        Status = status;
        Headers = headers;
        _body = body;
    }

    /// <summary>
    /// Deserializes the body returned by the API.
    /// </summary>
    /// <typeparam name="T">The type of the body.</typeparam>
    public T? GetBody<T>()
    {
        return _body.GetValue<T>();
    }

    /// <summary>
    /// Throws a <see cref="SurrealDbApiException"/> if <see cref="Status"/> is not in the 2xx range.
    /// </summary>
    /// <returns>The same response, so that calls can be chained.</returns>
    /// <exception cref="SurrealDbApiException"></exception>
    public SurrealDbApiResponse EnsureSuccessStatusCode()
    {
        if (!IsSuccessStatusCode)
        {
            throw new SurrealDbApiException(this);
        }

        return this;
    }
}
