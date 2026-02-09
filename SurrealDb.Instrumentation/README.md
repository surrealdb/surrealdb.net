# SurrealDB Instrumentation for OpenTelemetry

This is an [Instrumentation
Library](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/glossary.md#instrumentation-library),
which instruments and collects traces about database operations.

This component is based on
[v1.33](https://github.com/open-telemetry/semantic-conventions/blob/v1.33.0/docs/database/README.md)
of database semantic conventions. For details on the default set of
attributes that are added, check out the [Traces](#traces) and
[Metrics](#metrics) sections below.

## Steps to enable SurrealDb.Instrumentation

### Step 1: Install Package

Add a reference to the
[`SurrealDb.Instrumentation`](https://www.nuget.org/packages/SurrealDb.Instrumentation)
package. Also, add any other instrumentations & exporters you will need.

```shell
dotnet add package SurrealDb.Instrumentation
```

### Step 2: Enable SurrealDB Instrumentation at application startup

SurrealDB instrumentation must be enabled at application startup.

#### Traces

The following example demonstrates adding SurrealDB traces instrumentation
to a console application. This example also sets up the OpenTelemetry Console
exporter, which requires adding the package
[`OpenTelemetry.Exporter.Console`](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Console/README.md)
to the application.

```csharp
using OpenTelemetry.Trace;

public class Program
{
    public static void Main(string[] args)
    {
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSurrealDbClientInstrumentation()
            .AddConsoleExporter()
            .Build();
    }
}
```

The instrumentation adheres to the
[semantic conventions for database client spans][semconv-spans].
An activity emitted by the instrumentation will include the following list of
attributes:

* `error.type`
* `db.namespace`
* `db.operation.name`
* `db.query.summary`
* `db.query.text`
* `db.system.name`
* `server.address`
* `server.port`

#### Metrics

The following example demonstrates adding SurrealDB metrics instrumentation
to a console application. This example also sets up the OpenTelemetry Console
exporter, which requires adding the package
[`OpenTelemetry.Exporter.Console`](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.Console/README.md)
to the application.

```csharp
using OpenTelemetry.Metrics;

public class Program
{
    public static void Main(string[] args)
    {
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddSurrealDbClientInstrumentation()
            .AddConsoleExporter()
            .Build();
    }
}
```

The instrumentation adheres to the
[semantic conventions for database client metrics][semconv-metrics].

Currently, the instrumentation supports the following metric and attributes.

| Name | Instrument Type | Unit | Description |
| ---- | --------------- | ---- | ----------- |
| `db.client.operation.duration` | Histogram | `s` | Duration of database client operations. |

* `error.type`
* `db.namespace`
* `db.operation.name`
* `db.query.summary`
* `db.system.name`
* `server.address`
* `server.port`

#### ASP.NET Core

For an ASP.NET Core application, adding instrumentation is typically done in the
`ConfigureServices` of your `Startup` class. Refer to documentation for
[OpenTelemetry.Instrumentation.AspNetCore](../OpenTelemetry.Instrumentation.AspNetCore/README.md).

#### ASP.NET

For an ASP.NET application, adding instrumentation is typically done in the
`Global.asax.cs`. Refer to the documentation for
[OpenTelemetry.Instrumentation.AspNet](../OpenTelemetry.Instrumentation.AspNet/README.md).

## Advanced configuration

This instrumentation can be configured to change the default behavior by using
`SurrealDbClientTraceInstrumentationOptions`.

### RecordException

This option can be set to instruct the instrumentation to record Exceptions
as Activity
[events](https://github.com/open-telemetry/semantic-conventions/blob/main/docs/exceptions/exceptions-spans.md).

The default value is `false` and can be changed by the code like below.

```csharp
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSurrealDbClientInstrumentation(
        options => options.RecordException = true)
    .AddConsoleExporter()
    .Build();
```

### Filter

This option can be used to filter out activities based on the properties of the
`SurrealDbBeforeExecuteMethod` telemetry event being instrumented using a `Func<SurrealDbBeforeExecuteMethod, bool>`. The
function receives an instance of the `SurrealDbBeforeExecuteMethod` event and should return `true`
if the telemetry is to be collected, and `false` if it should not. The example below filters out all query methods.

```csharp
using var traceProvider = Sdk.CreateTracerProviderBuilder()
   .AddSqlClientInstrumentation(
       opt =>
       {
           opt.Filter = @event =>
           {
               return @event.Method != "query";
           };
       })
   .AddConsoleExporter()
   .Build();
```

## Experimental features

> [!NOTE]
> Experimental features are not enabled by default and can only be activated with
> environment variables. They are subject to change or removal in future releases.

### DB query parameters

The `OTEL_DOTNET_EXPERIMENTAL_SURREALDB_CLIENT_ENABLE_TRACE_DB_QUERY_PARAMETERS` environment
variable controls whether `db.query.parameter.<key>` attributes are emitted.

Query parameters may contain sensitive data, so only enable this experimental feature
if your queries and/or environment are appropriate for enabling this option.

`OTEL_DOTNET_EXPERIMENTAL_SURREALDB_CLIENT_ENABLE_TRACE_DB_QUERY_PARAMETERS` is implicitly
`false` by default. When set to `true`, the instrumentation will set
[`db.query.parameter.<key>`](https://github.com/open-telemetry/semantic-conventions/blob/main/docs/database/database-spans.md#span-definition)
attributes for each of the query parameters associated with a database command.

### Trace Context Propagation

Database trace context propagation can be enabled by setting
`OTEL_DOTNET_EXPERIMENTAL_SURREALDB_CLIENT_ENABLE_TRACE_CONTEXT_PROPAGATION`
environment variable to `true`.
This uses the [traceparent](https://www.w3.org/TR/trace-context/#traceparent-header)
information for the current connection.

## Activity Duration calculation

`Activity.Duration` represents the time the underlying connection takes to
execute the method/query. Completing the operation includes the time up to
determining that the request was successful. It doesn't include the time spent
reading the results from a query set (for example enumerating all the rows
returned by a data reader).

This is illustrated by the code snippet below:

```csharp
using var client = new SurrealDbConnection("...");

// Activity duration starts
var response = await client.Query("...");
// Activity duration ends

// Not included in the Activity duration
foreach (var result in response)
{
}
```

## References

* [OpenTelemetry Project](https://opentelemetry.io/)
* [Semantic conventions for database client spans][semconv-spans]
* [Semantic conventions for database client metrics][semconv-metrics]

[semconv-metrics]: https://github.com/open-telemetry/semantic-conventions/blob/v1.33.0/docs/database/database-metrics.md
[semconv-spans]: https://github.com/open-telemetry/semantic-conventions/blob/v1.33.0/docs/database/database-spans.md