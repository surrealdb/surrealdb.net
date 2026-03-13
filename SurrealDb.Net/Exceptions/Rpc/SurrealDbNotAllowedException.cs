using System.Collections.Generic;
using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: permission denied, method not allowed, function/scripting blocked.
/// </summary>
public sealed class SurrealDbNotAllowedException : SurrealDbRpcException
{
    public string? Kind { get; }

    /// <summary>
    /// True if the auth token has expired.
    /// </summary>
    public bool IsTokenExpired =>
        Kind == "Auth" && RpcErrorDetailHelpers.DetailInnerKind(Details) == "TokenExpired";

    /// <summary>
    /// True if authentication credentials are invalid.
    /// </summary>
    public bool IsInvalidAuth =>
        Kind == "Auth" && RpcErrorDetailHelpers.DetailInnerKind(Details) == "InvalidAuth";

    /// <summary>
    /// True if scripting is blocked.
    /// </summary>
    public bool IsScriptingBlocked => Kind == "Scripting";

    /// <summary>
    /// The method name that is not allowed, if applicable.
    /// </summary>
    public string? MethodName => RpcErrorDetailHelpers.DetailField(Details, "Method", "name");

    /// <summary>
    /// The function name that is not allowed, if applicable.
    /// </summary>
    public string? FunctionName => RpcErrorDetailHelpers.DetailField(Details, "Function", "name");

    internal SurrealDbNotAllowedException(
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
