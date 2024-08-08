namespace SurrealDb.Net.Tests.Models;

public class OperatorsRecordIdTests
{
    private static readonly RecordId RecordIdRef = new("table", "id");

    public static TheoryData<RecordId, RecordId?, bool> EqualityRecordIdCases =>
        new()
        {
            { RecordIdRef, RecordIdRef, true },
            { new RecordId("table", "id"), null, false },
            { new RecordId("table", "id"), new RecordId("table", "id"), true },
            { new RecordId("table", "1"), new RecordId("table", "2"), false },
            { new RecordId("table1", "id"), new RecordId("table2", "id"), false },
            { new RecordId("table1", "1"), new RecordId("table2", "2"), false },
            { new RecordId("table", "⟨42⟩"), new RecordId("table", "⟨42⟩"), true },
            { new RecordId("table", "⟨42⟩"), new RecordId("table", "42"), false },
            { new RecordId("table", "⟨42⟩"), new RecordId("table", "`42`"), true },
            { new RecordId("⟨42⟩", "id"), new RecordId("⟨42⟩", "id"), true },
            { new RecordId("⟨42⟩", "id"), new RecordId("42", "id"), false },
            { new RecordId("⟨42⟩", "id"), new RecordId("`42`", "id"), true },
        };

    [Theory]
    [MemberData(nameof(EqualityRecordIdCases))]
    public void ShouldBe(RecordId recordId1, RecordId recordId2, bool expected)
    {
        bool result = recordId1 == recordId2;

        result.Should().Be(expected);
    }
}
