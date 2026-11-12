namespace SurrealDb.AgentMemory;

/// <summary>
/// Path helpers for the Agent Memory API.
/// </summary>
public static class AgentMemoryPaths
{
    /// <summary>
    /// URL-encodes a single path segment, such as a context id or an entity name.
    /// </summary>
    /// <param name="value">Raw segment value.</param>
    public static string EncodePathSegment(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Uri.EscapeDataString(value);
    }

    /// <summary>
    /// Returns the API path prefix for an Agent Memory context: <c>/api/v1/{contextId}</c>.
    /// </summary>
    /// <param name="contextId">Context identifier.</param>
    public static string GetContextApiPrefix(string contextId)
    {
        return $"/api/v1/{EncodePathSegment(contextId)}";
    }
}
