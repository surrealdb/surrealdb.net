using SurrealDb.Net.Extensions;

namespace SurrealDb.Net.Tests.Extensions;

public class SemverExtensionsTests
{
    [Fact]
    public void ShouldParseSemverVersion()
    {
        var result = "surrealdb-1.0.0+20230913.54aedcd".ToSemver();

        result.Major.Should().Be(1);
        result.Minor.Should().Be(0);
        result.Patch.Should().Be(0);
        result.IsPrerelease.Should().BeFalse();
        result.IsRelease.Should().BeTrue();
        result.Metadata.Should().Be("20230913.54aedcd");
        result.MetadataIdentifiers.Should().HaveCount(2);
    }
}
