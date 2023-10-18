using System.Text;

namespace SurrealDb.Net.Tests;

public class SignUpTests
{
    [Theory]
    [InlineData("http://localhost:8000")]
    [InlineData("ws://localhost:8000/rpc")]
    public async Task ShouldSignUpUsingScopeAuth(string url)
    {
        Jwt? jwt = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/user.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;
            await client.Query(query);

            var authParams = new AuthParams
            {
                Namespace = dbInfo.Namespace,
                Database = dbInfo.Database,
                Scope = "user_scope",
                Username = "johndoe",
                Email = "john.doe@example.com",
                Password = "password123"
            };

            jwt = await client.SignUp(authParams);
        };

        await func.Should().NotThrowAsync();

        jwt.Should().NotBeNull();
        jwt!.Token.Should().BeValidJwt();
    }
}
