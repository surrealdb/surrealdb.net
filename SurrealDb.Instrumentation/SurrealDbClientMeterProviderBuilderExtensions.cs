using SurrealDb.Instrumentation.Internals;

namespace OpenTelemetry.Metrics;

/// <summary>
/// Extension methods to simplify registering of dependency instrumentation.
/// </summary>
public static class SurrealDbClientMeterProviderBuilderExtensions
{
    /// <summary>
    /// Enables SurrealDbClient instrumentation.
    /// </summary>
    /// <param name="builder"><see cref="MeterProviderBuilder"/> being configured.</param>
    /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
    public static MeterProviderBuilder AddSurrealDbClientInstrumentation(
        this MeterProviderBuilder builder
    )
    {
        builder.AddInstrumentation(sp =>
        {
            return SurrealDbClientInstrumentation.Instance.HandleManager.AddMetricHandle();
        });

        builder.AddMeter(SurrealDbTelemetryHelper.Meter.Name);

        return builder;
    }
}
