using System.Text.Json.Serialization.Metadata;

namespace SurrealDb.AgentMemory;

internal static class TransportExtensions
{
    /// <summary>
    /// Sends a request whose response must carry a body.
    /// </summary>
    public static async Task<T> RequestRequiredAsync<T>(
        this AgentMemoryTransport transport,
        HttpMethod method,
        string path,
        JsonTypeInfo<T> typeInfo,
        AgentMemoryRequestOptions? request,
        CancellationToken cancellationToken
    )
        where T : class
    {
        return await transport
                .RequestJsonAsync(method, path, typeInfo, request, cancellationToken)
                .ConfigureAwait(false)
            ?? throw new AgentMemoryException(
                0,
                "Unexpected response body",
                $"The server returned an empty body for {method.Method} {path}."
            );
    }

    /// <summary>
    /// Sends a request whose response body, if any, is ignored.
    /// </summary>
    public static async Task SendAsync(
        this AgentMemoryTransport transport,
        HttpMethod method,
        string path,
        AgentMemoryRequestOptions? request,
        CancellationToken cancellationToken
    )
    {
        await transport
            .RequestTextAsync(method, path, request, cancellationToken)
            .ConfigureAwait(false);
    }

    public static AgentMemoryException EmptyBody(string collection)
    {
        return new AgentMemoryException(
            0,
            "Unexpected response body",
            $"The server returned an empty body for the {collection} listing."
        );
    }

    public static AgentMemoryRequestOptions Query(QueryParameters query)
    {
        return new AgentMemoryRequestOptions { Query = query };
    }

    public static AgentMemoryRequestOptions Json(JsonBody body, bool idempotent = false)
    {
        return new AgentMemoryRequestOptions { Body = body.Build(), Idempotent = idempotent };
    }
}
