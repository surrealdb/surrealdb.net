using SurrealDb.Net.Models.Errors;

namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: duplicate resource (record, table, namespace, etc.).
/// </summary>
public sealed class SurrealDbAlreadyExistsException : SurrealDbRpcException
{
    private readonly AlreadyExistsErrorDetail? _details;

    public string? Kind { get; }

    /// <summary>
    /// The table name that was not found, if applicable.
    /// </summary>
    public string? TableName
    {
        get { return Kind != "Table" ? null : _details?.Name; }
    }

    /// <summary>
    /// The record ID that was not found, if applicable.
    /// </summary>
    public string? RecordId
    {
        get { return Kind != "Record" ? null : _details?.Id; }
    }

    internal SurrealDbAlreadyExistsException(
        string message,
        string? kind,
        AlreadyExistsErrorDetail? details
    )
        : base(message)
    {
        Kind = kind;
        _details = details;
    }
}
