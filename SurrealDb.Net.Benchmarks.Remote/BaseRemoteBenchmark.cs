using SurrealDb.Net.Benchmarks.Constants;
using SurrealDb.Net.Benchmarks.Models;
using SurrealDb.Net.Tests.Fixtures;

namespace SurrealDb.Net.Benchmarks.Remote;

public class BaseRemoteBenchmark : BaseBenchmark
{
    public static string Host { get; } = "127.0.0.1:8000";
    protected string HttpUrl { get; } = $"http://{Host}";
    protected string WsUrl { get; } = $"ws://{Host}/rpc";

    protected BaseRemoteBenchmark()
    {
        var isNativeAotRuntime = Environment.GetEnvironmentVariable(
            EnvVariablesConstants.NativeAotRuntime
        );
    }

    protected Task CreatePostTable(string url, DatabaseInfo databaseInfo)
    {
        var client = new SurrealDbClient(url, NamingPolicy);
        return CreatePostTable(client, databaseInfo);
    }

    protected Task CreateEcommerceTables(string url, DatabaseInfo databaseInfo)
    {
        var client = new SurrealDbClient(url, NamingPolicy);
        return CreateEcommerceTables(client, databaseInfo);
    }

    protected Task<IEnumerable<GeneratedPost>> SeedData(
        string url,
        DatabaseInfo databaseInfo,
        IEnumerable<GeneratedPost> posts
    )
    {
        var client = new SurrealDbClient(url, NamingPolicy);
        return SeedData(client, databaseInfo, posts);
    }

    protected Task<Post> GetFirstPost(string url, DatabaseInfo databaseInfo)
    {
        var client = new SurrealDbClient(url, NamingPolicy);
        return GetFirstPost(client, databaseInfo);
    }
}
