using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.Tests.Extensions;

public class FormattableStringExtensionsTests
{
    [Fact]
    public void ShouldReturnSameStringWithoutArguments()
    {
        FormattableString formattableString = $"DEFINE TABLE test;";

        var (query, @params) = formattableString.ExtractRawQueryParams();

        query.Should().Be("DEFINE TABLE test;");
        @params.Should().BeEmpty();
    }

    [Fact]
    public void ShouldExtractQueryWithOneArgument()
    {
        int value = 10;
        FormattableString formattableString = $"CREATE test SET value = {value};";

        var (query, @params) = formattableString.ExtractRawQueryParams();

        query.Should().Be("CREATE test SET value = $p0;");

        var expectedParams = new Dictionary<string, object> { { "p0", value } };
        @params.Should().BeEquivalentTo(expectedParams);
    }

    [Fact]
    public void ShouldExtractQueryWithMultipleArguments()
    {
        string table = "test";
        int value = 10;

        FormattableString formattableString = $"CREATE {table} SET value = {value};";

        var (query, @params) = formattableString.ExtractRawQueryParams();

        query.Should().Be("CREATE $p0 SET value = $p1;");

        var expectedParams = new Dictionary<string, object> { { "p0", table }, { "p1", value } };
        @params.Should().BeEquivalentTo(expectedParams);
    }

    [Fact]
    public void ShouldAvoidToDuplicateParamsWhenExtractingQueryParams()
    {
        string table = "test";
        FormattableString formattableString = $"""
            DEFINE TABLE {table};

            CREATE {table} SET value = {5};
            UPDATE {table} SET value = {10};
            DELETE {table};

            SELECT * FROM {table};
            """;

        var (query, @params) = formattableString.ExtractRawQueryParams();

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
