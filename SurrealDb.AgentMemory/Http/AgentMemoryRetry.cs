namespace SurrealDb.AgentMemory;

/// <summary>
/// The retry rules the transport applies.
/// </summary>
/// <remarks>
/// Retries apply to reads (<c>GET</c> and <c>HEAD</c>) and to writes explicitly marked idempotent,
/// on a <c>5xx</c> response or a connection failure.
/// A timeout and a caller's cancellation are never retried.
/// </remarks>
public static class AgentMemoryRetry
{
    private static readonly TimeSpan[] Backoff =
    [
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromMilliseconds(1000),
    ];

    /// <summary>
    /// The delay used for an attempt beyond the end of the back-off schedule.
    /// </summary>
    public static TimeSpan FallbackDelay { get; } = TimeSpan.FromMilliseconds(1000);

    /// <summary>
    /// The back-off delays used between retry attempts: 250ms, 500ms and 1000ms, capped at <paramref name="maxRetries"/>.
    /// </summary>
    /// <param name="maxRetries">The maximum number of retries.</param>
    public static IReadOnlyList<TimeSpan> BackoffSchedule(int maxRetries)
    {
        var capped = Math.Max(0, Math.Min(maxRetries, Backoff.Length));
        return Backoff[..capped];
    }

    /// <summary>
    /// Whether a failed request should be retried.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="status">The HTTP status, or <see langword="null"/> for a connection failure.</param>
    /// <param name="attempt">The zero-based attempt that just failed.</param>
    /// <param name="maxRetries">The maximum number of retries.</param>
    /// <param name="idempotent">Whether a write is safe to replay.</param>
    public static bool ShouldRetry(
        string method,
        int? status,
        int attempt,
        int maxRetries,
        bool idempotent = false
    )
    {
        if (attempt >= maxRetries)
        {
            return false;
        }

        var upper = method.ToUpperInvariant();
        if (upper is not ("GET" or "HEAD") && !idempotent)
        {
            return false;
        }

        return status is null || status >= 500;
    }

    internal static TimeSpan DelayFor(IReadOnlyList<TimeSpan> schedule, int attempt)
    {
        return attempt < schedule.Count ? schedule[attempt] : FallbackDelay;
    }
}
