using SurrealDb.Net.Models.Api;

namespace SurrealDb.Net.Exceptions;

/// <summary>
/// Thrown when an API defined with <c>DEFINE API</c> answers with a status outside the 2xx range.
/// </summary>
public sealed class SurrealDbApiException : SurrealDbException
{
    /// <summary>
    /// The unsuccessful response.
    /// </summary>
    public SurrealDbApiResponse Response { get; }

    internal SurrealDbApiException(SurrealDbApiResponse response)
        : base(
            $"The API {response.Method.ToUpperInvariant()} {response.Path} answered with status {response.Status}."
        )
    {
        Response = response;
    }
}
