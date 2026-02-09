namespace SurrealDb.Net.Telemetry.Events;

/// <summary>
/// Event triggered when a SurrealDB method failed.
/// </summary>
public sealed class SurrealDbExecuteError : ISurrealDbTelemetryEvent
{
    public const string Name = "SurrealDb.Error.Execute";

    public Exception? Exception { get; set; }
}
