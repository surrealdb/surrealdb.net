using SurrealDb.Net.Telemetry;

namespace SurrealDb.Instrumentation.Internals;

internal sealed class SurrealDbClientInstrumentation : IDisposable
{
    public static readonly SurrealDbClientInstrumentation Instance = new();
    public static SurrealDbClientTraceInstrumentationOptions TracingOptions { get; set; } = new();

    public readonly InstrumentationHandleManager HandleManager = new();

    private readonly SurrealDbEventHandler _handler = new();
    private readonly Task _task;

    /// <summary>
    /// Initializes a new instance of the <see cref="SurrealDbClientInstrumentation"/> class.
    /// </summary>
    private SurrealDbClientInstrumentation()
    {
        _task = Task.Factory.StartNew(ProcessAsync, TaskCreationOptions.LongRunning);
    }

    private async Task ProcessAsync()
    {
        await foreach (var @event in SurrealDbTelemetryChannel.ReadAllAsync().ConfigureAwait(false))
        {
            _handler.HandleEvent(@event);
        }
    }

    public void Dispose()
    {
        _task.Dispose();
    }
}
