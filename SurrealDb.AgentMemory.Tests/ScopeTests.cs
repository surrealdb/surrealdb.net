using FluentAssertions;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// Mirrors <c>packages/tests/memory/unit/scope.test.ts</c>, plus the .NET construction helpers.
/// </summary>
public class ScopeTests
{
    private static string[][]? Normalize(Scope? scope)
    {
        return Scope.Normalize(scope)?.Select(clause => clause.ToArray()).ToArray();
    }

    [Test]
    public void ReturnsNullForEmptyInputs()
    {
        Normalize(null).Should().BeNull();
        Normalize("").Should().BeNull();
        Normalize(Array.Empty<string>()).Should().BeNull();
        Normalize(Array.Empty<string[]>()).Should().BeNull();
    }

    [Test]
    public void WrapsABareStringAsASinglePathClause()
    {
        Normalize("team/eng").Should().BeEquivalentTo(new[] { new[] { "team/eng" } });
    }

    [Test]
    public void TurnsAFlatArrayIntoAnOrOfSingletonClauses()
    {
        Normalize(new[] { "a/1", "b/2" })
            .Should()
            .BeEquivalentTo(
                new[] { new[] { "a/1" }, new[] { "b/2" } },
                o => o.WithStrictOrdering()
            );
    }

    [Test]
    public void PassesANestedArrayThroughAsAnAndClause()
    {
        Normalize(new[] { new[] { "a", "b" } })
            .Should()
            .BeEquivalentTo(new[] { new[] { "a", "b" } }, o => o.WithStrictOrdering());
    }

    [Test]
    public void MixesSinglePathAndAndClauses()
    {
        Normalize(new[] { new[] { "a" }, new[] { "b", "c" }, new[] { "d" } })
            .Should()
            .BeEquivalentTo(
                new[] { new[] { "a" }, new[] { "b", "c" }, new[] { "d" } },
                o => o.WithStrictOrdering()
            );
    }

    [Test]
    public void DeduplicatesWithinAClauseKeepingFirstSeenOrder()
    {
        Normalize(new[] { new[] { "org/acme", "team/eng", "org/acme" } })
            .Should()
            .BeEquivalentTo(
                new[] { new[] { "org/acme", "team/eng" } },
                o => o.WithStrictOrdering()
            );
    }

    [Test]
    public void DropsEmptyPathsAndClausesThatBecomeEmpty()
    {
        Normalize(new[] { "", "team/eng" }).Should().BeEquivalentTo(new[] { new[] { "team/eng" } });
        Normalize(new[] { new[] { "", "" }, new[] { "team/eng" } })
            .Should()
            .BeEquivalentTo(new[] { new[] { "team/eng" } });
        Normalize(new[] { new[] { "" } }).Should().BeNull();
    }

    [Test]
    public void BuildsClausesWithTheFactoryMethods()
    {
        Normalize(Scope.Path("team/eng")).Should().BeEquivalentTo(new[] { new[] { "team/eng" } });
        Normalize(Scope.AnyOf("a", "b"))
            .Should()
            .BeEquivalentTo(new[] { new[] { "a" }, new[] { "b" } }, o => o.WithStrictOrdering());
        Normalize(Scope.AllOf("a", "b"))
            .Should()
            .BeEquivalentTo(new[] { new[] { "a", "b" } }, o => o.WithStrictOrdering());
        Normalize(
                new Scope(
                    new List<List<string>>
                    {
                        new() { "x" },
                        new() { "y", "z" },
                    }
                )
            )
            .Should()
            .BeEquivalentTo(
                new[] { new[] { "x" }, new[] { "y", "z" } },
                o => o.WithStrictOrdering()
            );
    }

    [Test]
    public void ConvertsNullImplicitlyToNull()
    {
        Scope? fromString = (string?)null;
        Scope? fromArray = (string[]?)null;

        fromString.Should().BeNull();
        fromArray.Should().BeNull();
    }
}
