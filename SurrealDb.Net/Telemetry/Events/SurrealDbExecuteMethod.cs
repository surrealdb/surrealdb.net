using SurrealDb.Net.Telemetry.Events.Data;

namespace SurrealDb.Net.Telemetry.Events;

/// <summary>
/// Event triggered when a SurrealDB method is executed from the client.
/// </summary>
public sealed class SurrealDbExecuteMethod : ISurrealDbTelemetryEvent
{
    public const string Name = "SurrealDb.Method.Execute";

    public string? Namespace { get; set; }
    public string? Database { get; set; }
    public TransientTraceData? Data { get; set; }
}
