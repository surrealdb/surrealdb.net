using System.Collections.Generic;

namespace SurrealDb.Net.Exceptions;

/// <summary>
/// Generated exception when the response from a SurrealDB method is a known error.
/// Supports a cause chain via <see cref="Cause"/> / <see cref="Exception.InnerException"/> (mirrors Rust's cause: Option&lt;Box&lt;Error&gt;&gt;).
/// </summary>
public abstract class SurrealDbRpcException : SurrealDbException
{
    /// <summary>Kind-specific structured details from the server (wire format: { "kind", "details" } or flat).</summary>
    protected internal IReadOnlyDictionary<string, object?>? Details { get; }

    /// <summary>
    /// The inner SurrealDB error that caused this one, if any. Use this to walk the cause chain (same as <see cref="Exception.InnerException"/> but typed).
    /// </summary>
    public SurrealDbException? Cause => base.InnerException as SurrealDbException;

    protected internal SurrealDbRpcException(
        string message,
        IReadOnlyDictionary<string, object?>? details = null,
        Exception? innerException = null
    )
        : base(message, innerException)
    {
        Details = details;
    }
}
