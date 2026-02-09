namespace SurrealDb.Net.Telemetry.Events.Data;

/// <summary>
/// Data holder for the current trace.
/// </summary>
public sealed class TransientTraceData
{
    public string? Traceparent { get; set; }
}
