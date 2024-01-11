using System.Text.RegularExpressions;

namespace SurrealDb.Net.Tests.Queryable.Functions;

public class RegexFunctionsTests : BaseQueryableTests
{
    [Test]
    public void Replace()
    {
        string query = ToSurql(Users.Select(p => new Regex(" ").Replace(p.Username, "_")));

        query
            .Should()
            .Be(
                """
                SELECT VALUE string::replace(Username, <regex> " ", "_") FROM user
                """
            );
    }

    [Test]
    public void ReplaceStatic()
    {
        string query = ToSurql(Users.Select(p => Regex.Replace(p.Username, " ", "_")));

        query
            .Should()
            .Be(
                """
                SELECT VALUE string::replace(Username, <regex> " ", "_") FROM user
                """
            );
    }
}
