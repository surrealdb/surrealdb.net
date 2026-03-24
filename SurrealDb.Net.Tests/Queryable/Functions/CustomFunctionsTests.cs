using SurrealDb.Net.Tests.Queryable.Models;

namespace SurrealDb.Net.Tests.Queryable.Functions;

public class CustomFunctionsTests : BaseQueryableTests
{
    [Test]
    public void BuiltInFunctionWithOneArg()
    {
        string query = ToSurql(
            Users.Select(u => SurrealDbTestFunctions.StringUppercase(u.Username))
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE string::uppercase(Username) FROM user
                """
            );
    }

    [Test]
    public void BuiltInFunctionWithNumericArg()
    {
        string query = ToSurql(Users.Where(u => SurrealDbTestFunctions.MathAbs(u.Age) > 0));

        query
            .Should()
            .Be(
                """
                SELECT Age, id, IsActive, IsAdmin, IsOwner, Tags, Username FROM user WHERE math::abs(<float> Age) > 0
                """
            );
    }

    [Test]
    public void UserDefinedFunctionWithNoArgs()
    {
        string query = ToSurql(Users.Select(u => SurrealDbTestFunctions.Greet()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE fn::greet() FROM user
                """
            );
    }

    [Test]
    public void UserDefinedFunctionWithOneArg()
    {
        string query = ToSurql(Users.Select(u => SurrealDbTestFunctions.Process(u.Username)));

        query
            .Should()
            .Be(
                """
                SELECT VALUE fn::process(Username) FROM user
                """
            );
    }

    [Test]
    public void UserDefinedFunctionWithManyArgs()
    {
        string query = ToSurql(
            Users.Select(u => SurrealDbTestFunctions.Combine(u.Username, "separator", u.Username))
        );

        query
            .Should()
            .Be(
                """
                SELECT VALUE fn::combine(Username, "separator", Username) FROM user
                """
            );
    }

    [Test]
    public void UserDefinedFunctionAsExtensionMethod()
    {
        string query = ToSurql(Users.Select(u => u.Followers()));

        query
            .Should()
            .Be(
                """
                SELECT VALUE fn::followers($this) FROM user
                """
            );
    }
}
