using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Models.Api;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net;

/// <summary>
/// Invokes the APIs defined with <c>DEFINE API</c>, in the current session and transaction.
/// </summary>
/// <example>
/// <code>
/// var response = await client.Api().Get("/users/1");
/// var user = response.EnsureSuccessStatusCode().GetBody&lt;User&gt;();
/// </code>
/// </example>
public sealed class SurrealDbApi
{
    // 💡 The body has its own statement, so it can be deserialized into any type, even on an error status
    private const string INVOKE_QUERY = """
        LET $api_response = api::invoke($path, $request);
        RETURN $api_response.status;
        RETURN $api_response.headers;
        RETURN $api_response.body;
        """;

    private readonly ISurrealDbSharedMethods _client;
    private readonly string _prefix;

    // 💡 Header names are case-insensitive
    private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

    internal SurrealDbApi(ISurrealDbSharedMethods client, string? prefix)
    {
        _client = client;
        _prefix = prefix?.TrimEnd('/') ?? string.Empty;
        if (_prefix.Length > 0 && !_prefix.StartsWith("/", StringComparison.Ordinal))
        {
            _prefix = $"/{_prefix}";
        }
    }

    /// <summary>
    /// Sets a header sent with every request of this instance.
    /// </summary>
    /// <param name="name">The name of the header.</param>
    /// <param name="value">The value of the header, or <c>null</c> to stop sending it.</param>
    /// <returns>The same instance, so that calls can be chained.</returns>
    public SurrealDbApi Header(string name, string? value)
    {
        if (value is null)
        {
            _headers.Remove(name);
        }
        else
        {
            _headers[name] = value;
        }

        return this;
    }

    /// <summary>
    /// Invokes an API with the given request.
    /// </summary>
    /// <param name="path">The path of the API, appended to the prefix of this instance.</param>
    /// <param name="request">The method, body, headers and query parameters. Defaults to a <c>GET</c>.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="SurrealDbException">The API could not be invoked, e.g. it is not permitted.</exception>
    public async Task<SurrealDbApiResponse> Invoke(
        string path,
        SurrealDbApiRequest? request = null,
        CancellationToken cancellationToken = default
    )
    {
        request ??= new SurrealDbApiRequest();

        string fullPath = NormalizePath(path);
        string method = request.Method.ToLowerInvariant();

        var headers = new Dictionary<string, string>(_headers, StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            headers[header.Key] = header.Value;
        }

        var invokeRequest = new Dictionary<string, object?>
        {
            { "method", method },
            { "headers", headers },
            { "query", new Dictionary<string, string>(request.Query) },
        };
        if (request.HasBody)
        {
            invokeRequest.Add("body", request.Body);
        }

        var response = await _client
            .RawQuery(
                INVOKE_QUERY,
                new Dictionary<string, object?>
                {
                    { "path", fullPath },
                    { "request", invokeRequest },
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        if (response.FirstError is { } error)
        {
            throw new SurrealDbErrorResultException(error);
        }

        int status = response.GetValue<int>(1);
        // 💡 Header names are case-insensitive
        var responseHeaders = new Dictionary<string, string>(
            response.GetValue<Dictionary<string, string>>(2) ?? new Dictionary<string, string>(),
            StringComparer.OrdinalIgnoreCase
        );
        var body = (SurrealDbOkResult)response[3];

        return new SurrealDbApiResponse(fullPath, method, status, responseHeaders, body);
    }

    /// <summary>
    /// Invokes an API with a <c>GET</c> request.
    /// </summary>
    public Task<SurrealDbApiResponse> Get(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        return Invoke(path, new SurrealDbApiRequest { Method = "get" }, cancellationToken);
    }

    /// <summary>
    /// Invokes an API with a <c>POST</c> request without a body.
    /// </summary>
    public Task<SurrealDbApiResponse> Post(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        return Invoke(path, new SurrealDbApiRequest { Method = "post" }, cancellationToken);
    }

    /// <summary>
    /// Invokes an API with a <c>POST</c> request.
    /// </summary>
    /// <param name="path">The path of the API, appended to the prefix of this instance.</param>
    /// <param name="body">The body of the request. <c>null</c> is sent as <c>NULL</c>.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    public Task<SurrealDbApiResponse> Post(
        string path,
        object? body,
        CancellationToken cancellationToken = default
    )
    {
        return Invoke(
            path,
            new SurrealDbApiRequest { Method = "post", Body = body },
            cancellationToken
        );
    }

    /// <summary>
    /// Invokes an API with a <c>PUT</c> request without a body.
    /// </summary>
    public Task<SurrealDbApiResponse> Put(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        return Invoke(path, new SurrealDbApiRequest { Method = "put" }, cancellationToken);
    }

    /// <summary>
    /// Invokes an API with a <c>PUT</c> request.
    /// </summary>
    /// <param name="path">The path of the API, appended to the prefix of this instance.</param>
    /// <param name="body">The body of the request. <c>null</c> is sent as <c>NULL</c>.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    public Task<SurrealDbApiResponse> Put(
        string path,
        object? body,
        CancellationToken cancellationToken = default
    )
    {
        return Invoke(
            path,
            new SurrealDbApiRequest { Method = "put", Body = body },
            cancellationToken
        );
    }

    /// <summary>
    /// Invokes an API with a <c>DELETE</c> request without a body.
    /// </summary>
    public Task<SurrealDbApiResponse> Delete(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        return Invoke(path, new SurrealDbApiRequest { Method = "delete" }, cancellationToken);
    }

    /// <summary>
    /// Invokes an API with a <c>DELETE</c> request.
    /// </summary>
    /// <param name="path">The path of the API, appended to the prefix of this instance.</param>
    /// <param name="body">The body of the request. <c>null</c> is sent as <c>NULL</c>.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    public Task<SurrealDbApiResponse> Delete(
        string path,
        object? body,
        CancellationToken cancellationToken = default
    )
    {
        return Invoke(
            path,
            new SurrealDbApiRequest { Method = "delete", Body = body },
            cancellationToken
        );
    }

    /// <summary>
    /// Invokes an API with a <c>PATCH</c> request without a body.
    /// </summary>
    public Task<SurrealDbApiResponse> Patch(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        return Invoke(path, new SurrealDbApiRequest { Method = "patch" }, cancellationToken);
    }

    /// <summary>
    /// Invokes an API with a <c>PATCH</c> request.
    /// </summary>
    /// <param name="path">The path of the API, appended to the prefix of this instance.</param>
    /// <param name="body">The body of the request. <c>null</c> is sent as <c>NULL</c>.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    public Task<SurrealDbApiResponse> Patch(
        string path,
        object? body,
        CancellationToken cancellationToken = default
    )
    {
        return Invoke(
            path,
            new SurrealDbApiRequest { Method = "patch", Body = body },
            cancellationToken
        );
    }

    /// <summary>
    /// Invokes an API with a <c>TRACE</c> request without a body.
    /// </summary>
    public Task<SurrealDbApiResponse> Trace(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        return Invoke(path, new SurrealDbApiRequest { Method = "trace" }, cancellationToken);
    }

    /// <summary>
    /// Invokes an API with a <c>TRACE</c> request.
    /// </summary>
    /// <param name="path">The path of the API, appended to the prefix of this instance.</param>
    /// <param name="body">The body of the request. <c>null</c> is sent as <c>NULL</c>.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    public Task<SurrealDbApiResponse> Trace(
        string path,
        object? body,
        CancellationToken cancellationToken = default
    )
    {
        return Invoke(
            path,
            new SurrealDbApiRequest { Method = "trace", Body = body },
            cancellationToken
        );
    }

    private string NormalizePath(string path)
    {
        string normalizedPath = path.StartsWith("/", StringComparison.Ordinal) ? path : $"/{path}";

        if (_prefix.Length == 0)
        {
            return normalizedPath;
        }

        // 💡 The root of a prefix is the prefix itself, e.g. Api("/users").Get("/") invokes "/users"
        return normalizedPath == "/" ? _prefix : $"{_prefix}{normalizedPath}";
    }
}
