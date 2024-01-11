namespace SurrealDb.Net.Internals.Ws;

internal class Pinger : IDisposable
{
    private static readonly TimeSpan _timerInterval = TimeSpan.FromSeconds(30);

    private readonly Func<CancellationToken, Task> _pingCallback;
#if NET5_0_OR_GREATER
    private readonly PeriodicTimer _timer = new(_timerInterval);
    private Task? _timerTask;
    private CancellationTokenSource? _cancellationTokenSource;
#else
    private Timer? _timer;
#endif

    public Pinger(Func<CancellationToken, Task> pingCallback)
    {
        _pingCallback = pingCallback;
    }

    public void Start()
    {
#if NET5_0_OR_GREATER
        if (_timerTask is not null)
        {
            _cancellationTokenSource = new();
            _timerTask = ExecuteAsync();
        }
#else
        if (_timer != null)
        {
            _timer = new(Execute, null, TimeSpan.Zero, _timerInterval);
        }
#endif
    }

    public void Dispose()
    {
#if NET5_0_OR_GREATER
        _timerTask?.Dispose();
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
#endif
        _timer?.Dispose();
    }

#if NET5_0_OR_GREATER
    private async Task ExecuteAsync()
    {
        try
        {
            while (
                await _timer
                    .WaitForNextTickAsync(_cancellationTokenSource?.Token ?? default)
                    .ConfigureAwait(false)
            )
            {
                await _pingCallback(default).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
    }
#else
    private void Execute(object state)
    {
        _ = _pingCallback(default);
    }
#endif
}
