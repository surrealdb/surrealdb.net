using SurrealDb.Net.Models.Errors;

namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: resource not found (table, record, namespace, method, etc.).
/// </summary>
public sealed class SurrealDbNotFoundException : SurrealDbRpcException
{
    private readonly NotFoundErrorDetail? _details;

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

    /// <summary>
    /// The RPC method name that was not found, if applicable.
    /// </summary>
    public string? MethodName
    {
        get { return Kind != "Method" ? null : _details?.Name; }
    }

    /// <summary>
    /// The namespace name that was not found, if applicable.
    /// </summary>
    public string? NamespaceName
    {
        get { return Kind != "Namespace" ? null : _details?.Name; }
    }

    /// <summary>
    /// The database name that was not found, if applicable.
    /// </summary>
    public string? DatabaseName
    {
        get { return Kind != "Database" ? null : _details?.Name; }
    }

    internal SurrealDbNotFoundException(string message, string? kind, NotFoundErrorDetail? details)
        : base(message)
    {
        Kind = kind;
        _details = details;
    }
}
