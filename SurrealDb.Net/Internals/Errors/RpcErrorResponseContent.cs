using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Exceptions.Rpc;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Models.Errors;

namespace SurrealDb.Net.Internals.Errors;

internal sealed class RpcErrorResponseContent
{
    public long Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Kind { get; set; }
    public RpcErrorDetails? Details { get; set; }

    /// <summary>
    /// Optional inner error forming a recursive cause chain (mirrors Rust's cause: Option&lt;Box&lt;Error&gt;&gt;).
    /// </summary>
    public RpcErrorResponseContent? Cause { get; set; }

    public SurrealDbException ToException()
    {
        var kind = RpcErrorKindExtensions.From(Kind, Code);
        var detailKind = Details?.Kind;
        var detailsDict = Details?.Details;
        var inner = Cause?.ToException();

        return kind switch
        {
            RpcErrorKind.Validation => new SurrealDbValidationException(
                Message,
                detailKind,
                detailsDict,
                inner
            ),
            RpcErrorKind.NotFound => new SurrealDbNotFoundException(
                Message,
                detailKind,
                detailsDict,
                inner
            ),
            RpcErrorKind.NotAllowed => new SurrealDbNotAllowedException(
                Message,
                detailKind,
                detailsDict,
                inner
            ),
            RpcErrorKind.Configuration => new SurrealDbConfigurationException(
                Message,
                detailKind,
                inner
            ),
            RpcErrorKind.Connection => new SurrealDbConnectionException(Message, detailKind, inner),
            RpcErrorKind.Query => new SurrealDbQueryException(
                Message,
                detailKind,
                detailsDict,
                inner
            ),
            RpcErrorKind.Thrown => new SurrealDbThrownException(Message, inner),
            RpcErrorKind.Serialization => new SurrealDbSerializationException(
                Message,
                detailKind,
                inner
            ),
            RpcErrorKind.AlreadyExists => new SurrealDbAlreadyExistsException(
                Message,
                detailKind,
                detailsDict,
                inner
            ),
            _ => new SurrealDbInternalException(Message, inner),
        };
    }
}
