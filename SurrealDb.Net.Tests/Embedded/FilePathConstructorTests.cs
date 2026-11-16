using SurrealDb.Embedded.RocksDb;
using SurrealDb.Embedded.SurrealKv;

namespace SurrealDb.Net.Tests.Embedded;

/// <summary>
/// The file path constructors prefix the engine scheme. Passing a full endpoint
/// must not produce "rocksdb://rocksdb://..." (os error 123 on Windows, see #271).
/// </summary>
public class FilePathConstructorTests
{
    [Test]
    [Arguments("data.db", "rocksdb://data.db")]
    [Arguments("rocksdb://data.db", "rocksdb://data.db")]
    [Arguments("RocksDB://data.db", "rocksdb://data.db")]
    [Arguments("C:/data/data.db", "rocksdb://C:/data/data.db")]
    public async Task RocksDbShouldNotPrefixTheSchemeTwice(string filePath, string expected)
    {
        await using var client = new SurrealDbRocksDbClient(filePath);

        client.Uri.OriginalString.Should().Be(expected);
    }

    [Test]
    [Arguments("data.db", "surrealkv://data.db")]
    [Arguments("surrealkv://data.db", "surrealkv://data.db")]
    [Arguments("SurrealKV://data.db", "surrealkv://data.db")]
    [Arguments("C:/data/data.db", "surrealkv://C:/data/data.db")]
    public async Task SurrealKvShouldNotPrefixTheSchemeTwice(string filePath, string expected)
    {
        await using var client = new SurrealDbKvClient(filePath);

        client.Uri.OriginalString.Should().Be(expected);
    }

    [Test]
    public async Task RocksDbShouldCreateTheDatabaseInTheGivenFolderFromAnEndpoint()
    {
        await ShouldCreateTheDatabaseInTheGivenFolder(path => new SurrealDbRocksDbClient(
            $"rocksdb://{path}"
        ));
    }

    [Test]
    public async Task SurrealKvShouldCreateTheDatabaseInTheGivenFolderFromAnEndpoint()
    {
        await ShouldCreateTheDatabaseInTheGivenFolder(path => new SurrealDbKvClient(
            $"surrealkv://{path}"
        ));
    }

    private static async Task ShouldCreateTheDatabaseInTheGivenFolder(
        Func<string, ISurrealDbClient> createClient
    )
    {
        string folder = Path.Combine(Path.GetTempPath(), $"surrealdb-net-{Guid.NewGuid():N}");
        string path = Path.Combine(folder, "data.db");

        try
        {
            await using (var client = createClient(path))
            {
                await client.Use("test", "test");
            }

            // The folder alone is not enough: PreConnect creates it before the engine starts
            Directory.Exists(path).Should().BeTrue();
            Directory.EnumerateFileSystemEntries(path).Should().NotBeEmpty();
        }
        finally
        {
            try
            {
                Directory.Delete(folder, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
