using System.Collections.Generic;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Helpers;

namespace SurrealDb.Net.Exceptions.Rpc;

/// <summary>
/// Server error: duplicate resource (record, table, namespace, etc.).
/// </summary>
public sealed class SurrealDbAlreadyExistsException : SurrealDbRpcException
{
    public string? Kind { get; }

    /// <summary>
    /// The table name that already exists, if applicable.
    /// </summary>
    public string? TableName => RpcErrorDetailHelper.DetailField(Details, "Table", "name");

    /// <summary>
    /// The record ID that already exists, if applicable.
    /// </summary>
    public string? RecordId => RpcErrorDetailHelper.DetailField(Details, "Record", "id");

    internal SurrealDbAlreadyExistsException(
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
