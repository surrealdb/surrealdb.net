namespace SurrealDb.Net.Telemetry.Events;

/// <summary>
/// Event triggered after any SurrealDB method is executed from the client.
/// </summary>
public sealed class SurrealDbAfterExecuteMethod : ISurrealDbTelemetryEvent
{
    public const string Name = "SurrealDb.Method.AfterExecute";

    public int? BatchSize { get; set; }
}
