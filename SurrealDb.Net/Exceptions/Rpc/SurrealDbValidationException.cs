using SurrealDb.Net.Models.Errors;

namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: validation failure (parse error, invalid request/params, bad input).
/// </summary>
public sealed class SurrealDbValidationException : SurrealDbRpcException
{
    private readonly ValidationErrorDetail? _details;

    public string? Kind { get; }

    /// <summary>
    /// True if this is a SurrealQL parse error.
    /// </summary>
    public bool IsParseError => Kind == "Parse";

    /// <summary>
    /// The name of the invalid parameter, if applicable.
    /// </summary>
    public string? ParameterName
    {
        get { return Kind != "InvalidParameter" ? null : _details?.Name; }
    }

    internal SurrealDbValidationException(
        string message,
        string? kind,
        ValidationErrorDetail? details
    )
        : base(message)
    {
        Kind = kind;
        _details = details;
    }
}
