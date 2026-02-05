namespace SurrealDb.Net.Telemetry.Events;

/// <summary>
/// Event triggered before any SurrealDB method is executed from the client.
/// </summary>
public sealed class SurrealDbBeforeExecuteMethod : ISurrealDbTelemetryEvent
{
    public const string Name = "SurrealDb.Method.BeforeExecute";

    public string Summary { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public Uri? Address { get; set; }
    public string? Table { get; set; }
}
