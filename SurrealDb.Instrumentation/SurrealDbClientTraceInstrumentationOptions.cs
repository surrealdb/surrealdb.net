using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using SurrealDb.Instrumentation.Internals;
using SurrealDb.Net.Telemetry.Events;

namespace SurrealDb.Instrumentation;

/// <summary>
/// Options for <see cref="SurrealDbClientInstrumentation"/>.
/// </summary>
public class SurrealDbClientTraceInstrumentationOptions
{
    private const string ContextPropagationLevelEnvVar =
        "OTEL_DOTNET_EXPERIMENTAL_SURREALDB_CLIENT_ENABLE_TRACE_CONTEXT_PROPAGATION";
    private const string SetDbQueryParametersEnvVar =
        "OTEL_DOTNET_EXPERIMENTAL_SURREALDB_CLIENT_ENABLE_TRACE_DB_QUERY_PARAMETERS";

    /// <summary>
    /// Initializes a new instance of the <see cref="SurrealDbClientTraceInstrumentationOptions"/> class.
    /// </summary>
    public SurrealDbClientTraceInstrumentationOptions()
        : this(new ConfigurationBuilder().AddEnvironmentVariables().Build()) { }

    private SurrealDbClientTraceInstrumentationOptions(IConfiguration configuration)
    {
        if (
            bool.TryParse(
                configuration[ContextPropagationLevelEnvVar],
                out bool enableTraceContextPropagationValue
            )
        )
        {
            this.EnableTraceContextPropagation = enableTraceContextPropagationValue;
        }
        else
        {
            SurrealDbInstrumentationEventSource.Log.LogInvalidConfigurationValue(
                ContextPropagationLevelEnvVar,
                configuration[ContextPropagationLevelEnvVar]!
            );
        }

        if (
            bool.TryParse(
                configuration[SetDbQueryParametersEnvVar],
                out bool setDbQueryParametersValue
            )
        )
        {
            this.SetDbQueryParameters = setDbQueryParametersValue;
        }
        else
        {
            SurrealDbInstrumentationEventSource.Log.LogInvalidConfigurationValue(
                SetDbQueryParametersEnvVar,
                configuration[SetDbQueryParametersEnvVar]!
            );
        }
    }

    /// <summary>
    /// Gets or sets a filter function that determines whether or not to collect telemetry about a method.
    /// </summary>
    /// <remarks>
    /// Notes:
    /// <list type="bullet">
    /// <item>The first parameter passed to the filter function is the event triggered before the method is being executed.</item>
    /// <item>The return value for the filter function is interpreted as:
    /// <list type="bullet">
    /// <item>If filter returns <see langword="true" />, the method is collected.</item>
    /// <item>If filter returns <see langword="false" /> or throws an exception the method is NOT collected.</item>
    /// </list></item>
    /// </list>
    /// </remarks>
    public Func<SurrealDbBeforeExecuteMethod, bool>? Filter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the exception will be
    /// recorded as <see cref="ActivityEvent"/> or not.
    /// Default value: <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>For specification details see: <see
    /// href="https://github.com/open-telemetry/semantic-conventions/blob/main/docs/exceptions/exceptions-spans.md"/>.</para>
    /// </remarks>
    public bool RecordException { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not the <see cref="SurrealDbClientInstrumentation"/>
    /// should add the names and values of query parameters as the <c>db.query.parameter.{key}</c> tag.
    /// Default value: <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>WARNING: SetDbQueryParameters will capture the raw
    /// <c>Value</c>. Make sure your query parameters never
    /// contain any sensitive data.</b>
    /// </para>
    /// </remarks>
    internal bool SetDbQueryParameters { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to send traceparent information to a SurrealDB database.
    /// </summary>
    internal bool EnableTraceContextPropagation { get; set; }
}
