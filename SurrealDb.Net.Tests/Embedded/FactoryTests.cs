namespace SurrealDb.Net.Tests.Embedded;

public class FactoryTests
{
    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    public async Task ShouldCreateMultipleClientInstances(string connectionString)
    {
        IEnumerable<Post>? list1 = null;
        IEnumerable<Post>? list2 = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator1 = new SurrealDbClientGenerator();
            using var client1 = surrealDbClientGenerator1.Create(connectionString);
            await client1.Use("test", "test");

            await using var surrealDbClientGenerator2 = new SurrealDbClientGenerator();
            using var client2 = surrealDbClientGenerator1.Create(connectionString);
            await client2.Use("test", "test");

            await client1.Create("data", new Post { Content = "First post" });
            await client2.Create("data", new Post { Content = "Second post" });

            list1 = await client1.Select<Post>("data");
            list2 = await client2.Select<Post>("data");
        };

        await func.Should().NotThrowAsync();

        list1.Should().NotBeNull().And.HaveCount(1);
        list2.Should().NotBeNull().And.HaveCount(1);
    }
}
