using SurrealDb.Net.Handlers;

namespace SurrealDb.Net.Tests.Handlers;

public class QueryInterpolatedStringHandlerTests
{
    private (string, IReadOnlyDictionary<string, object?>) HandleQuery(
        QueryInterpolatedStringHandler handler
    )
    {
        return (handler.FormattedText, handler.Parameters);
    }

    [Fact]
    public void ShouldReturnSameStringWithoutArguments()
    {
        var (query, @params) = HandleQuery($"DEFINE TABLE test;");

        query.Should().Be("DEFINE TABLE test;");
        @params.Should().BeEmpty();
    }

    [Fact]
    public void ShouldExtractQueryWithOneArgument()
    {
        int value = 10;

        var (query, @params) = HandleQuery($"CREATE test SET value = {value};");

        query.Should().Be("CREATE test SET value = $p0;");

        var expectedParams = new Dictionary<string, object> { { "p0", value } };
        @params.Should().BeEquivalentTo(expectedParams);
    }

    [Fact]
    public void ShouldExtractQueryWithMultipleArguments()
    {
        string table = "test";
        int value = 10;

        var (query, @params) = HandleQuery($"CREATE {table} SET value = {value};");

        query.Should().Be("CREATE $p0 SET value = $p1;");

        var expectedParams = new Dictionary<string, object> { { "p0", table }, { "p1", value } };
        @params.Should().BeEquivalentTo(expectedParams);
    }

    [Fact]
    public void ShouldAvoidToDuplicateParamsWhenExtractingQueryParams()
    {
        string table = "test";

        var (query, @params) = HandleQuery(
            $"""
            DEFINE TABLE {table};

            CREATE {table} SET value = {5};
            UPDATE {table} SET value = {10};
            DELETE {table};

            SELECT * FROM {table};
            """
        );

        query
            .Should()
            .Be(
                """
                DEFINE TABLE $p0;

                CREATE $p0 SET value = $p1;
                UPDATE $p0 SET value = $p2;
                DELETE $p0;

                SELECT * FROM $p0;
                """
            );

        var expectedParams = new Dictionary<string, object>
        {
            { "p0", table },
            { "p1", 5 },
            { "p2", 10 }
        };
        @params.Should().BeEquivalentTo(expectedParams);
    }
}
