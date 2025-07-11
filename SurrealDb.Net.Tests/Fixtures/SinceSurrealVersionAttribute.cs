using Semver;

namespace SurrealDb.Net.Tests.Fixtures;

public sealed class SinceSurrealVersionAttribute(string version)
    : SkipAttribute($"This test is only supported with Surreal v{version} or later.")
{
    public override async Task<bool> ShouldSkip(BeforeTestContext context)
    {
        var expectedVersion = SemVersion.Parse(version, SemVersionStyles.Any);

        int index = context
            .TestDetails.TestMethod.Parameters.Index()
            .First(x => x.Item.Name == "connectionString")
            .Index;

        var connectionString = context.TestDetails.TestMethodArguments[index] as string;
        var currentVersion = await SurrealDbClientGenerator.GetSurrealTestVersion(
            connectionString!
        );

        bool shouldExecute = SemVersion.CompareSortOrder(currentVersion, expectedVersion) >= 0;
        return !shouldExecute;
    }
}
