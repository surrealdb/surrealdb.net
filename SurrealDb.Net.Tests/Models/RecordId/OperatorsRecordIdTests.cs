namespace SurrealDb.Net.Tests.Models;

public class OperatorsRecordIdTests
{
    private static readonly RecordIdOfString RecordIdRef = new("table", "id");

    public static TheoryData<RecordId, RecordId?, bool> EqualityRecordIdCases =>
        new()
        {
            { RecordIdRef, RecordIdRef, true },
            { new RecordIdOfString("table", "id"), null, false },
            { new RecordIdOfString("table", "id"), new RecordIdOfString("table", "id"), true },
            { new RecordIdOfString("table", "1"), new RecordIdOfString("table", "2"), false },
            { new RecordIdOfString("table1", "id"), new RecordIdOfString("table2", "id"), false },
            { new RecordIdOfString("table1", "1"), new RecordIdOfString("table2", "2"), false },
        };

    [Theory]
    [MemberData(nameof(EqualityRecordIdCases))]
    public void ShouldBe(RecordId recordId1, RecordId? recordId2, bool expected)
    {
        bool result = recordId1 == recordId2!;

        result.Should().Be(expected);
    }
}
