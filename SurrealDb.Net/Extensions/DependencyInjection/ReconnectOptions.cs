namespace SurrealDb.Net.Extensions.DependencyInjection;

/// <summary>
/// How a WebSocket client reconnects after the connection is lost.
/// Each failed attempt waits longer than the previous one, up to <see cref="RetryDelayMax"/>.
/// </summary>
public sealed class ReconnectOptions
{
    private static readonly Random _random = new();

    /// <summary>
    /// Reconnect after the connection was unexpectedly lost. Defaults to <c>true</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How many failed attempts are made before giving up, or <c>-1</c> to never give up.
    /// Defaults to <c>-1</c>.
    /// Once the client gives up, the next request tries to connect again.
    /// </summary>
    public int Attempts { get; set; } = -1;

    /// <summary>
    /// The delay before the second attempt. The first attempt is made right away. Defaults to 1 second.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The longest delay between two attempts. Defaults to 60 seconds.
    /// </summary>
    public TimeSpan RetryDelayMax { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// How much the delay grows after each failed attempt. Defaults to 2.
    /// </summary>
    public double RetryDelayMultiplier { get; set; } = 2;

    /// <summary>
    /// A random offset applied to each delay, as a fraction of it. Defaults to 0.1 (± 10%).
    /// </summary>
    public double RetryDelayJitter { get; set; } = 0.1;

    public ReconnectOptions() { }

    public ReconnectOptions(ReconnectOptions clone)
    {
        Enabled = clone.Enabled;
        Attempts = clone.Attempts;
        RetryDelay = clone.RetryDelay;
        RetryDelayMax = clone.RetryDelayMax;
        RetryDelayMultiplier = clone.RetryDelayMultiplier;
        RetryDelayJitter = clone.RetryDelayJitter;
    }

    internal bool AllowsAttempt(int failedAttempts)
    {
        return Enabled && (Attempts < 0 || failedAttempts < Attempts);
    }

    /// <summary>
    /// The delay to wait after <paramref name="failedAttempts"/> failed attempts.
    /// </summary>
    /// <param name="failedAttempts">The number of failed attempts, starting at 1.</param>
    /// <param name="random">A random number between 0 and 1, used for the jitter.</param>
    internal TimeSpan GetDelay(int failedAttempts, double random)
    {
        double delayMs =
            RetryDelay.TotalMilliseconds
            * Math.Pow(RetryDelayMultiplier, Math.Max(0, failedAttempts - 1));
        double jitter = (random * 2 - 1) * RetryDelayJitter;
        double maxMs = RetryDelayMax.TotalMilliseconds;

        return TimeSpan.FromMilliseconds(Math.Max(0, Math.Min(delayMs * (1 + jitter), maxMs)));
    }

    internal TimeSpan GetDelay(int failedAttempts)
    {
        double random;
        lock (_random)
        {
            random = _random.NextDouble();
        }

        return GetDelay(failedAttempts, random);
    }

    internal void Validate()
    {
        if (Attempts < -1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Attempts),
                "The number of attempts must be -1 or more."
            );
        }
        if (RetryDelay < TimeSpan.Zero || RetryDelayMax < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RetryDelay),
                "Retry delays cannot be negative."
            );
        }
        if (RetryDelayMultiplier < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RetryDelayMultiplier),
                "The retry delay multiplier must be 1 or more."
            );
        }
        if (RetryDelayJitter is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RetryDelayJitter),
                "The retry delay jitter must be between 0 and 1."
            );
        }
    }
}
