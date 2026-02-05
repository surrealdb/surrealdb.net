using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SurrealDb.Instrumentation.Internals;

internal static class SurrealDbTelemetryHelper
{
    public const string SurrealDbSystemName = "surrealdb";

    private static readonly (ActivitySource ActivitySource, Meter Meter) Telemetry =
        CreateTelemetry();
    public static readonly ActivitySource ActivitySource = Telemetry.ActivitySource;
    public static readonly Meter Meter = Telemetry.Meter;

    public static readonly Histogram<double> DbClientOperationDuration = Meter.CreateHistogram(
        "db.client.operation.duration",
        unit: "s",
        description: "Duration of database client operations.",
        advice: new InstrumentAdvice<double>
        {
            HistogramBucketBoundaries = [0.001, 0.005, 0.01, 0.05, 0.1, 0.5, 1, 5, 10],
        }
    );

    private static (ActivitySource ActivitySource, Meter Meter) CreateTelemetry()
    {
        const string telemetrySchemaUrl = "https://opentelemetry.io/schemas/1.33.0";
        const string version = ThisAssembly.Project.Version;

        var activitySourceOptions = new ActivitySourceOptions(ActivitySourceName)
        {
            Version = version,
            TelemetrySchemaUrl = telemetrySchemaUrl,
        };

        var meterOptions = new MeterOptions(ActivitySourceName)
        {
            Version = version,
            TelemetrySchemaUrl = telemetrySchemaUrl,
        };

        return (new ActivitySource(activitySourceOptions), new Meter(meterOptions));
    }

    public const string ActivitySourceName = ThisAssembly.Project.AssemblyName;

    public static Activity? StartActivity(string summary)
    {
        return ActivitySource.StartActivity(summary, ActivityKind.Client);
    }
}
