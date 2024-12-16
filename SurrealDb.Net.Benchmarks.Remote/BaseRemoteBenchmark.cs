using SurrealDb.Net.Benchmarks.Constants;
using SurrealDb.Net.Benchmarks.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Tests.Fixtures;

namespace SurrealDb.Net.Benchmarks.Remote;

public class BaseRemoteBenchmark : BaseBenchmark
{
    private const string Host = "127.0.0.1:8000";
    protected string HttpUrl => $"http://{Host}";
    protected string WsUrl => $"ws://{Host}/rpc";

    protected BaseRemoteBenchmark()
    {
        var isNativeAotRuntime = Environment.GetEnvironmentVariable(
            EnvVariablesConstants.NativeAotRuntime
        );
    }

    protected async Task CreatePostTable(string url, DatabaseInfo databaseInfo)
    {
        await using var client = await CreateClient(url, databaseInfo);
        await CreatePostTable(client, databaseInfo);
    }

    protected async Task CreateEcommerceTables(string url, DatabaseInfo databaseInfo)
    {
        await using var client = await CreateClient(url, databaseInfo);
        await CreateEcommerceTables(client, databaseInfo);
    }

    protected async Task<IEnumerable<GeneratedPost>> SeedData(
        string url,
        DatabaseInfo databaseInfo,
        IEnumerable<GeneratedPost> posts
    )
    {
        await using var client = await CreateClient(url, databaseInfo);
        return await SeedData(client, databaseInfo, posts);
    }

    protected async Task<Post> GetFirstPost(string url, DatabaseInfo databaseInfo)
    {
        await using var client = await CreateClient(url, databaseInfo);
        return await GetFirstPost(client, databaseInfo);
    }

    private async Task<SurrealDbClient> CreateClient(string url, DatabaseInfo databaseInfo)
    {
        var client = new SurrealDbClient(url, NamingPolicy);
        await client.SignIn(new RootAuth { Username = "root", Password = "root" });
        await client.Use(databaseInfo.Namespace, databaseInfo.Database);

        return client;
    }
}
