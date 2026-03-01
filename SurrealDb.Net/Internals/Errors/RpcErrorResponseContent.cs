using Dahomey.Cbor;
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

    public SurrealDbException ToException(CborOptions cborOptions)
    {
        var kind = RpcErrorKindExtensions.From(Kind, Code);
        var innerDetails = Details?.Details;

        return kind switch
        {
            RpcErrorKind.Validation => new SurrealDbValidationException(
                Message,
                innerDetails?.Kind,
                innerDetails?.Details.HasValue == true
                    ? CborSerializer.Deserialize<ValidationErrorDetail>(
                        innerDetails.Details.Value.Span,
                        cborOptions
                    )
                    : null
            ),
            RpcErrorKind.NotFound => new SurrealDbNotFoundException(
                Message,
                innerDetails?.Kind,
                innerDetails?.Details.HasValue == true
                    ? CborSerializer.Deserialize<NotFoundErrorDetail>(
                        innerDetails.Details.Value.Span,
                        cborOptions
                    )
                    : null
            ),
            RpcErrorKind.NotAllowed => new SurrealDbNotAllowedException(
                Message,
                innerDetails?.Kind,
                innerDetails?.Details.HasValue == true
                    ? CborSerializer.Deserialize<NotAllowedErrorDetail>(
                        innerDetails.Details.Value.Span,
                        cborOptions
                    )
                    : null
            ),
            RpcErrorKind.Configuration => new SurrealDbConfigurationException(
                Message,
                innerDetails?.Kind
            ),
            RpcErrorKind.Connection => new SurrealDbConnectionException(
                Message,
                innerDetails?.Kind
            ),
            RpcErrorKind.Query => new SurrealDbQueryException(
                Message,
                innerDetails?.Kind,
                innerDetails?.Details.HasValue == true
                    ? CborSerializer.Deserialize<QueryErrorDetail>(
                        innerDetails.Details.Value.Span,
                        cborOptions
                    )
                    : null
            ),
            RpcErrorKind.Thrown => new SurrealDbThrownException(Message),
            RpcErrorKind.Serialization => new SurrealDbSerializationExeption(
                Message,
                innerDetails?.Kind
            ),
            RpcErrorKind.AlreadyExists => new SurrealDbAlreadyExistsException(
                Message,
                innerDetails?.Kind,
                innerDetails?.Details.HasValue == true
                    ? CborSerializer.Deserialize<AlreadyExistsErrorDetail>(
                        innerDetails.Details.Value.Span,
                        cborOptions
                    )
                    : null
            ),
            _ => new SurrealDbInternalException(Message),
        };
    }
}
