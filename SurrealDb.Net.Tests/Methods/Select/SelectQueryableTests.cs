namespace SurrealDb.Net.Tests.Methods.Select;

public class SelectQueryableTests
{
    //[Theory]
    //[InlineData("http://127.0.0.1:8000")]
    //[InlineData("ws://127.0.0.1:8000/rpc")]
    //public async Task ShouldSelectWithComplexQuery(string url)
    //{
    //    IEnumerable<string>? result = null;

    //    Func<Task> func = async () =>
    //    {
    //        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
    //        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

    //        string filePath = Path.Combine(
    //            AppDomain.CurrentDomain.BaseDirectory,
    //            "Schemas/post.surql"
    //        );
    //        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

    //        string query = fileContent;

    //        using var client = surrealDbClientGenerator.Create(url);
    //        await client.SignIn(new RootAuth { Username = "root", Password = "root" });
    //        await client.Use(dbInfo.Namespace, dbInfo.Database);
    //        await client.Query(query);

    //        // TODO : Do not use post but another table with large number of entries
    //        result = await client
    //            .Select<Post>("post")
    //            .Where(p => p.Status == "DRAFT")
    //            .OrderBy(p => p.Id)
    //            .ThenBy(p => p.Title)
    //            .Skip(1)
    //            .Take(5)
    //            .Select(p => p.Content)
    //            .ToListAsync();
    //    };

    //    await func.Should().NotThrowAsync();

    //    result.Should().NotBeNull().And.HaveCount(2);
    //}
}
