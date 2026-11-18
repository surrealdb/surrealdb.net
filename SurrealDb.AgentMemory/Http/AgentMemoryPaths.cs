namespace SurrealDb.AgentMemory;

/// <summary>
/// Path helpers for the Agent Memory API.
/// </summary>
public static class AgentMemoryPaths
{
    /// <summary>
    /// URL-encodes a single path segment, such as a context id or an entity name.
    /// </summary>
    /// <remarks>
    /// An empty segment, <c>.</c> and <c>..</c> are refused: URL resolution would drop them or step up a level
    /// (even percent-encoded), so a request for one would reach a different route.
    /// </remarks>
    /// <param name="value">Raw segment value.</param>
    /// <exception cref="ArgumentException">The value is empty, <c>.</c> or <c>..</c>.</exception>
    public static string EncodePathSegment(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value is "" or "." or "..")
        {
            throw new ArgumentException(
                $"'{value}' cannot be used as a path segment: it would address a different route.",
                nameof(value)
            );
        }

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
