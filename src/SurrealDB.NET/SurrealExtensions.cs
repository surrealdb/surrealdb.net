using System.Text.Json.Serialization;

using Microsoft.Extensions.DependencyInjection;

using SurrealDB.NET.Json;
using SurrealDB.NET.TextRpc;

namespace SurrealDB.NET;

public static class SurrealExtensions
{
    public static IServiceCollection AddSurrealDB(this IServiceCollection services, Action<SurrealOptions>? configure = null)
    {
        services.AddOptions();
        services.AddLogging();

        services.AddOptions<SurrealOptions>()
            .BindConfiguration(SurrealOptions.Section)
            .Configure(options =>
            {
                options.JsonRequestOptions.Converters.Add(new SurrealTimeSpanJsonConverter());
                options.JsonRequestOptions.Converters.Add(new SurrealRecordIdJsonConverter());
            })
            .PostConfigure(configure ?? (static _ => { }));

        services.AddKeyedSingleton<ISurrealClient, SurrealTextRpcClient>("root");
        services.AddScoped<ISurrealClient, SurrealTextRpcClient>();

        return services;
    }
}
