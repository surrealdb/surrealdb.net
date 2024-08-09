namespace SurrealDb.Net.Tests.Models;

public class OperatorsStringRecordIdTests
{
    public static TheoryData<StringRecordId, StringRecordId, bool> EqualityCases =>
        new()
        {
            { new StringRecordId("table:id"), new StringRecordId("table:id"), true },
            { new StringRecordId("table:one"), new StringRecordId("table:two"), false },
        };

    [Theory]
    [MemberData(nameof(EqualityCases))]
    public void ShouldBe(StringRecordId recordId1, StringRecordId recordId2, bool expected)
    {
        bool result = recordId1 == recordId2;

        result.Should().Be(expected);
    }
}
