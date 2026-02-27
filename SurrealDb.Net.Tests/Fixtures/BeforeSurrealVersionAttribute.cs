using Semver;

namespace SurrealDb.Net.Tests.Fixtures;

public sealed class BeforeSurrealVersionAttribute(string version)
    : SkipAttribute($"This test is only supported before Surreal v{version}.")
{
    public override async Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        var expectedVersion = SemVersion.Parse(version, SemVersionStyles.Any);

        int index = context
            .TestDetails.MethodMetadata.Parameters.Index()
            .First(x => x.Item.Name == "connectionString")
            .Index;

        var connectionString = context.TestDetails.TestMethodArguments[index] as string;
        var currentVersion = await SurrealDbClientGenerator.GetSurrealTestVersion(
            connectionString!
        );

        bool shouldExecute =
            SemVersion.CompareSortOrder(
                currentVersion.WithoutPrereleaseOrMetadata(),
                expectedVersion
            ) < 0;
        return !shouldExecute;
    }
}
