using SurrealDb.Net.Models.Response;
using SurrealDb.Net.Telemetry;
using SurrealDb.Net.Telemetry.Events;

namespace SurrealDb.Net;

public abstract partial class BaseSurrealDbClient
{
    private async Task RunWithTelemetryAsync(
        Task method,
        string methodName,
        string? tableOrFunction = null
    )
    {
        await StartTelemetryAsync(methodName, tableOrFunction).ConfigureAwait(false);

        try
        {
            await method.ConfigureAwait(false);

            await SurrealDbTelemetryChannel
                .WriteAsync(new SurrealDbAfterExecuteMethod())
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await SurrealDbTelemetryChannel
                .WriteAsync(new SurrealDbExecuteError { Exception = ex })
                .ConfigureAwait(false);
            throw;
        }
    }

    private async Task<T> RunWithTelemetryAsync<T>(
        Task<T> method,
        string methodName,
        string? tableOrFunction = null
    )
    {
        await StartTelemetryAsync(methodName, tableOrFunction).ConfigureAwait(false);

        try
        {
            var response = await method.ConfigureAwait(false);

            await SurrealDbTelemetryChannel
                .WriteAsync(new SurrealDbAfterExecuteMethod())
                .ConfigureAwait(false);

            return response;
        }
        catch (Exception ex)
        {
            await SurrealDbTelemetryChannel
                .WriteAsync(new SurrealDbExecuteError { Exception = ex })
                .ConfigureAwait(false);
            throw;
        }
    }

    private async Task<SurrealDbResponse> RunQueryWithTelemetryAsync(
        Task<SurrealDbResponse> method,
        string query,
        IReadOnlyDictionary<string, object?> parameters
    )
    {
        const string methodName = "query";

        await StartTelemetryAsync(methodName, null).ConfigureAwait(false);

        await SurrealDbTelemetryChannel
            .WriteAsync(new SurrealDbBeforeExecuteQuery { Query = query, Parameters = parameters })
            .ConfigureAwait(false);

        try
        {
            var response = await method.ConfigureAwait(false);

            await SurrealDbTelemetryChannel
                .WriteAsync(new SurrealDbAfterExecuteMethod { BatchSize = response.Count })
                .ConfigureAwait(false);

            return response;
        }
        catch (Exception ex)
        {
            await SurrealDbTelemetryChannel
                .WriteAsync(new SurrealDbExecuteError { Exception = ex })
                .ConfigureAwait(false);
            throw;
        }
    }

    private async Task StartTelemetryAsync(string methodName, string? tableOrFunction)
    {
        await SurrealDbTelemetryChannel
            .WriteAsync(
                new SurrealDbBeforeExecuteMethod
                {
                    Address = Uri,
                    Method = methodName,
                    Summary = CreateSummary(methodName, tableOrFunction),
                    Table = tableOrFunction,
                }
            )
            .ConfigureAwait(false);
    }

    private static string CreateSummary(string methodName, string? tableOrFunction)
    {
        return string.IsNullOrEmpty(tableOrFunction)
            ? $"{methodName};"
            : $"{methodName} {tableOrFunction};";
    }
}
