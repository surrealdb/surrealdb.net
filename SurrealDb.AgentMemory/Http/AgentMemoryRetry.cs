namespace SurrealDb.AgentMemory;

/// <summary>
/// The retry rules the transport applies.
/// </summary>
/// <remarks>
/// Retries apply to reads (<c>GET</c> and <c>HEAD</c>) and to writes explicitly marked idempotent,
/// on a <c>5xx</c> response or a connection failure.
/// A timeout and a caller's cancellation are never retried, and neither are <c>501</c> and <c>505</c>,
/// which describe the request rather than the moment.
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
    /// The longest wait a <c>Retry-After</c> header can ask of a retry. A longer value is cut to this, so a
    /// response cannot stall the caller for an unbounded time.
    /// </summary>
    public static TimeSpan MaxRetryAfter { get; } = TimeSpan.FromSeconds(10);

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

        // 501 (not implemented) and 505 (HTTP version not supported) will not change on a second try.
        return status is null || (status >= 500 && status is not (501 or 505));
    }

    /// <summary>
    /// Whether a response says that an earlier request with the same idempotency key is still running.
    /// The service answers that with a <c>409</c> and expects the caller to come back, by which time the
    /// first request has finished and its stored response is returned.
    /// </summary>
    internal static bool IsStillInProgress(int status, string? body)
    {
        return status == 409
            && body is not null
            && body.Contains("already in progress", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The wait before the next attempt: the scheduled back-off, raised to what a <c>Retry-After</c> header
    /// asked for (up to <see cref="MaxRetryAfter"/>). A shorter wait would land while the service is still
    /// shedding load.
    /// </summary>
    internal static TimeSpan DelayFor(
        IReadOnlyList<TimeSpan> schedule,
        int attempt,
        TimeSpan? retryAfter = null
    )
    {
        var backoff = attempt < schedule.Count ? schedule[attempt] : FallbackDelay;
        if (retryAfter is not { } asked)
        {
            return backoff;
        }

        var bounded = asked < MaxRetryAfter ? asked : MaxRetryAfter;
        return bounded > backoff ? bounded : backoff;
    }
}
