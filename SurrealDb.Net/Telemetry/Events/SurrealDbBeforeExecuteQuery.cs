namespace SurrealDb.Net.Telemetry.Events;

/// <summary>
/// Additional information from an event triggered before a "query" method is executed.
/// </summary>
public sealed class SurrealDbBeforeExecuteQuery : ISurrealDbTelemetryEvent
{
    public const string Name = "SurrealDb.Query.BeforeExecute";

    public string? Query { get; set; }
    public IReadOnlyDictionary<string, object?>? Parameters { get; set; }
}
