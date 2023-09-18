using Microsoft.Extensions.DependencyInjection;
using SurrealDB.NET.BinaryRpc;
using SurrealDB.NET.Http;
using SurrealDB.NET.Json;
using SurrealDB.NET.TextRpc;
using System.Net.Http.Headers;

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
                options.JsonRequestOptions.Converters.Add(new SurrealTableJsonConverter());
                options.JsonRequestOptions.Converters.Add(new SurrealThingJsonConverter());
            })
            .PostConfigure(configure ?? (static _ => { }));

		services.AddHttpClient<SurrealJsonHttpClient>();
		services.AddScoped<ISurrealHttpClient>(di => di.GetRequiredService<SurrealJsonHttpClient>());
		services.AddScoped<SurrealTextRpcClient>();
		services.AddKeyedScoped<ISurrealRpcClient>("text", (di, _) => di.GetRequiredService<SurrealTextRpcClient>());
		services.AddScoped<SurrealBinaryRpcClient>();
		services.AddKeyedScoped<ISurrealRpcClient>("binary", (di, _) => di.GetRequiredService<SurrealBinaryRpcClient>());

        return services;
    }
}
