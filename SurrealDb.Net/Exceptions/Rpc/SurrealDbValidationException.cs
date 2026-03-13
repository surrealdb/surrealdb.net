using System.Collections.Generic;
using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: validation failure (parse error, invalid request/params, bad input).
/// </summary>
public sealed class SurrealDbValidationException : SurrealDbRpcException
{
    public string? Kind { get; }

    /// <summary>
    /// True if this is a SurrealQL parse error.
    /// </summary>
    public bool IsParseError => Kind == "Parse";

    /// <summary>
    /// The name of the invalid parameter, if applicable.
    /// </summary>
    public string? ParameterName =>
        RpcErrorDetailHelpers.DetailField(Details, "InvalidParameter", "name");

    internal SurrealDbValidationException(
        string message,
        string? kind,
        IReadOnlyDictionary<string, object?>? details,
        Exception? innerException = null
    )
        : base(message, details, innerException)
    {
        Kind = kind;
    }
}
