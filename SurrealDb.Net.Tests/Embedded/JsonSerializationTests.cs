namespace SurrealDb.Net.Tests.Embedded;

public class JsonSerializationTests
{
    [Theory]
    [InlineData("Endpoint=mem://;User=root;Pass=root;Serialization=JSON")]
    public async Task JsonSerializationIsNotSupportedByEmbeddedProviders(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
        };

        await func.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("The JSON serialization is not supported for the in-memory provider.");
    }
}
