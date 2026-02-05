using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SurrealDb.Instrumentation;
using SurrealDb.Instrumentation.Internals;

namespace OpenTelemetry.Trace;

/// <summary>
/// Extension methods to simplify registering of dependency instrumentation.
/// </summary>
public static class TracerProviderBuilderExtensions
{
    /// <summary>
    /// Enables SurrealDbClient instrumentation.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> being configured.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddSurrealDbClientInstrumentation(
        this TracerProviderBuilder builder
    ) =>
        AddSurrealDbClientInstrumentation(
            builder,
            name: null,
            configureSurrealDbClientTraceInstrumentationOptions: null
        );

    /// <summary>
    /// Enables SurrealDbClient instrumentation.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> being configured.</param>
    /// <param name="configureSurrealDbClientTraceInstrumentationOptions">Callback action for configuring <see cref="SurrealDbClientTraceInstrumentationOptions"/>.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddSurrealDbClientInstrumentation(
        this TracerProviderBuilder builder,
        Action<SurrealDbClientTraceInstrumentationOptions> configureSurrealDbClientTraceInstrumentationOptions
    ) =>
        AddSurrealDbClientInstrumentation(
            builder,
            name: null,
            configureSurrealDbClientTraceInstrumentationOptions
        );

    /// <summary>
    /// Enables SurrealDbClient instrumentation.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> being configured.</param>
    /// <param name="name">Name which is used when retrieving options.</param>
    /// <param name="configureSurrealDbClientTraceInstrumentationOptions">Callback action for configuring <see cref="SurrealDbClientTraceInstrumentationOptions"/>.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddSurrealDbClientInstrumentation(
        this TracerProviderBuilder builder,
        string? name,
        Action<SurrealDbClientTraceInstrumentationOptions>? configureSurrealDbClientTraceInstrumentationOptions
    )
    {
        name ??= Options.DefaultName;

        if (configureSurrealDbClientTraceInstrumentationOptions != null)
        {
            builder.ConfigureServices(services =>
                services.Configure(name, configureSurrealDbClientTraceInstrumentationOptions)
            );
        }

        builder.AddInstrumentation(sp =>
        {
            var surrealDbOptions = sp.GetRequiredService<
                IOptionsMonitor<SurrealDbClientTraceInstrumentationOptions>
            >()
                .Get(name);
            SurrealDbClientInstrumentation.TracingOptions = surrealDbOptions;
            return SurrealDbClientInstrumentation.Instance.HandleManager.AddTracingHandle();
        });

        string[] sources =
        [
            SurrealDbTelemetryHelper.ActivitySourceName,
            SurrealDbTelemetryHelper.SurrealDbSystemName,
        ];
        builder.AddSource(sources);

        return builder;
    }
}
