using System.Net;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Exceptions.Methods;

/// <summary>
/// Thrown when an import fails, either as a whole or because some of its statements failed.
/// </summary>
public sealed class SurrealDbImportException : SurrealDbMethodException
{
    /// <summary>
    /// The HTTP status code returned by the server.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// The statements that failed, in the order they were run.
    /// Empty when the import was rejected as a whole, e.g. on a parse error or a missing <c>OPTION IMPORT</c>.
    /// </summary>
    public IReadOnlyList<ISurrealDbErrorResult> Errors { get; }

    internal SurrealDbImportException(
        HttpStatusCode statusCode,
        IReadOnlyList<ISurrealDbErrorResult> errors,
        string message
    )
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}
