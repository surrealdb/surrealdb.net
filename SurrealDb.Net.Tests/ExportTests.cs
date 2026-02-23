using System.Text;
using System.Text.RegularExpressions;
using Semver;

namespace SurrealDb.Net.Tests;

public class ExportTests
{
    private readonly VerifySettings _verifySettings = new();

    public ExportTests()
    {
        _verifySettings.DisableRequireUniquePrefix();
        _verifySettings.UseDirectory("Snapshots");
        _verifySettings.IgnoreParametersForVerified();

        // 💡 "ScrubInlineDateTimes" won't work as DateTime cannot be parsed with more than 7 seconds fraction units
        _verifySettings.AddScrubber(
            (sb, counter) =>
            {
                const string pattern = @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{3,9}Z";
                var matches = Regex.Matches(sb.ToString(), pattern);

                foreach (Match match in matches)
                {
                    string value = match.Value;

                    var date = DateTime.Parse(value[..^4] + "Z");
                    int id = counter.Next(date);

                    string name = $"DateTime_{id}";

                    sb.Replace(value, name);
                }
            }
        );
        _verifySettings.AddScrubber(
            (sb, _) =>
            {
                // 💡 Ensures line return doesn't breaking Snapshot testing
                sb.Replace("\n\n\n\n", "\n\n");
            }
        );
    }

    private static bool IsV3Compatible(SemVersion version)
    {
        return version.Major == 2 && version.Minor >= 6;
    }

    private void UseSnapshotDirectory(SemVersion version)
    {
        var majorVersion = IsV3Compatible(version) switch
        {
            true => 3,
            false => version.Major,
        };

        string versionFolder = $"v{majorVersion}";
        _verifySettings.UseDirectory($"Snapshots/{versionFolder}");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("2.0")]
    public async Task ShouldExportEmptyDatabaseWithDefaultOptions(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);
        UseSnapshotDirectory(version);

        string? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            result = await client.Export();
        };

        await func.Should().NotThrowAsync();

        await Verify(result, _verifySettings);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("2.0")]
    public async Task ShouldExportFullDatabaseWithDefaultOptions(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);
        UseSnapshotDirectory(version);

        string? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Delete("post");

            var post = new Post
            {
                Id = ("post", "dotnet-123456"),
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK",
            };

            await client.Create(post);

            result = await client.Export();
        };

        await func.Should().NotThrowAsync();

        await Verify(result, _verifySettings);
    }
}
