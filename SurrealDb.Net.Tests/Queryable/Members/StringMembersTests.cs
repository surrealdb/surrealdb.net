namespace SurrealDb.Net.Tests.Queryable.Members;

public class StringMembersTests : BaseQueryableTests
{
    [Test]
    public void Empty()
    {
        string query = ToSurql(Users.Select(p => string.Empty));

        query
            .Should()
            .Be(
                """
                SELECT VALUE "" FROM user
                """
            );
    }

    [Test]
    public void LengthFromConstant()
    {
        string query = ToSurql(Users.Select(p => "12345".Length));

        query
            .Should()
            .Be(
                """
                SELECT VALUE 5 FROM user
                """
            );
    }

    [Test]
    public void Length()
    {
        var str = "12345";
        string query = ToSurql(Users.Select(p => str.Length));

        query
            .Should()
            .Be(
                """
                SELECT VALUE string::len($str) FROM user
                """
            );
    }
}
