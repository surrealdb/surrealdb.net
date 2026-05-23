namespace SurrealDb.Net.Tests.Queryable.Visitors;

public class ToRecordIdVisitorTests : BaseQueryableTests
{
    [Test]
    public void Simple()
    {
        RecordId recordId = ("user", "hksov829u8s4lhehf5hw");
        string query = ToSurql(Users.Where(u => u.Id! == recordId));

        query
            .Should()
            .Be(
                """
                SELECT Age, id, IsActive, IsAdmin, IsOwner, Tags, Username FROM user:`hksov829u8s4lhehf5hw`
                """
            );
    }

    [Test]
    public void WithAndCondition()
    {
        RecordId recordId = ("user", "hksov829u8s4lhehf5hw");
        var date = new DateTime(2025, 1, 1);

        string query = ToSurql(Users.Where(u => DateTime.Now > date && u.Id! == recordId));

        query
            .Should()
            .Be(
                """
                SELECT Age, id, IsActive, IsAdmin, IsOwner, Tags, Username FROM user:`hksov829u8s4lhehf5hw` WHERE time::now() > $date
                """
            );
    }

    [Test]
    public void WithOrCondition()
    {
        RecordId recordId = ("user", "hksov829u8s4lhehf5hw");
        var date = new DateTime(2025, 1, 1);

        string query = ToSurql(Users.Where(u => DateTime.Now > date || u.Id! == recordId));

        query
            .Should()
            .Be(
                """
                SELECT Age, id, IsActive, IsAdmin, IsOwner, Tags, Username FROM user WHERE time::now() > $date || id == $recordId
                """
            );
    }

    [Test]
    public void Redundant()
    {
        RecordId recordId = ("user", "hksov829u8s4lhehf5hw");
        RecordId recordId2 = ("user", "hksov829u8s4lhehf5hw");
        string query = ToSurql(Users.Where(u => u.Id! == recordId).Where(u => u.Id! == recordId2));

        query
            .Should()
            .Be(
                """
                SELECT Age, id, IsActive, IsAdmin, IsOwner, Tags, Username FROM user:`hksov829u8s4lhehf5hw`
                """
            );
    }

    [Test]
    public void ExclusiveFromNoRecordKeyMatch()
    {
        RecordId recordId = ("user", "hksov829u8s4lhehf5hw");
        RecordId recordId2 = ("user", "apbxmhfowxsjb0qyre9x");
        string query = ToSurql(Users.Where(u => u.Id! == recordId).Where(u => u.Id! == recordId2));

        query
            .Should()
            .Be(
                """
                SELECT Age, id, IsActive, IsAdmin, IsOwner, Tags, Username FROM user:`hksov829u8s4lhehf5hw` WHERE id == $recordId2
                """
            );
    }

    [Test]
    public void ExclusiveFromNoTableMatch()
    {
        RecordId recordId = ("post", "hksov829u8s4lhehf5hw");
        string query = ToSurql(Users.Where(u => u.Id! == recordId));

        query
            .Should()
            .Be(
                """
                SELECT Age, id, IsActive, IsAdmin, IsOwner, Tags, Username FROM user WHERE id == $recordId
                """
            );
    }
}
