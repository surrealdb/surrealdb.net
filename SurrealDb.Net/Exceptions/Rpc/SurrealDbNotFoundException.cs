using System.Collections.Generic;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Helpers;

namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: resource not found (table, record, namespace, method, etc.).
/// </summary>
public sealed class SurrealDbNotFoundException : SurrealDbRpcException
{
    public string? Kind { get; }

    /// <summary>
    /// The table name that was not found, if applicable.
    /// </summary>
    public string? TableName => RpcErrorDetailHelper.DetailField(Details, "Table", "name");

    /// <summary>
    /// The record ID that was not found, if applicable.
    /// </summary>
    public string? RecordId => RpcErrorDetailHelper.DetailField(Details, "Record", "id");

    /// <summary>
    /// The RPC method name that was not found, if applicable.
    /// </summary>
    public string? MethodName => RpcErrorDetailHelper.DetailField(Details, "Method", "name");

    /// <summary>
    /// The namespace name that was not found, if applicable.
    /// </summary>
    public string? NamespaceName => RpcErrorDetailHelper.DetailField(Details, "Namespace", "name");

    /// <summary>
    /// The database name that was not found, if applicable.
    /// </summary>
    public string? DatabaseName => RpcErrorDetailHelper.DetailField(Details, "Database", "name");

    internal SurrealDbNotFoundException(
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
