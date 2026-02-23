using Semver;

namespace SurrealDb.Net.Tests.Fixtures;

public sealed class SinceSurrealVersionAttribute(string version)
    : SkipAttribute($"This test is only supported with Surreal v{version} or later.")
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
            ) >= 0;
        return !shouldExecute;
    }
}
