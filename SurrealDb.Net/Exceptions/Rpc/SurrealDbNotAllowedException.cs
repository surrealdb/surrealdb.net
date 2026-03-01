using SurrealDb.Net.Models.Errors;

namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: permission denied, method not allowed, function/scripting blocked.
/// </summary>
public sealed class SurrealDbNotAllowedException : SurrealDbRpcException
{
    private readonly NotAllowedErrorDetail? _details;

    public string? Kind { get; }

    /// <summary>
    /// True if the auth token has expired.
    /// </summary>
    public bool IsTokenExpired => Kind == "Auth" && _details?.Auth?.Kind == "TokenExpired";

    /// <summary>
    /// True if authentication credentials are invalid.
    /// </summary>
    public bool IsInvalidAuth => Kind == "Auth" && _details?.Auth?.Kind == "InvalidAuth";

    /// <summary>
    /// True if scripting is blocked.
    /// </summary>
    public bool IsScriptingBlocked => Kind == "Auth" && _details?.Auth?.Kind == "Scripting";

    /// <summary>
    /// The method name that is not allowed, if applicable.
    /// </summary>
    public string? MethodName
    {
        get { return Kind != "Method" ? null : _details?.Name; }
    }

    /// <summary>
    /// The function name that is not allowed, if applicable.
    /// </summary>
    public string? FunctionName
    {
        get { return Kind != "Function" ? null : _details?.Name; }
    }

    internal SurrealDbNotAllowedException(
        string message,
        string? kind,
        NotAllowedErrorDetail? details
    )
        : base(message)
    {
        Kind = kind;
        _details = details;
    }
}
