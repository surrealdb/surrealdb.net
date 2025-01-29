namespace SurrealDb.Net.Tests.Models;

public class OperatorsRecordIdTests
{
    private static readonly RecordIdOfString RecordIdRef = new("table", "id");

    public static class TestDataSources
    {
        public static IEnumerable<Func<(RecordId, RecordId?, bool)>> EqualityRecordIdCases()
        {
            yield return () => (RecordIdRef, RecordIdRef, true);
            yield return () => (new RecordIdOfString("table", "id"), null, false);
            yield return () =>
                (new RecordIdOfString("table", "id"), new RecordIdOfString("table", "id"), true);
            yield return () =>
                (new RecordIdOfString("table", "1"), new RecordIdOfString("table", "2"), false);
            yield return () =>
                (new RecordIdOfString("table1", "id"), new RecordIdOfString("table2", "id"), false);
            yield return () =>
                (new RecordIdOfString("table1", "1"), new RecordIdOfString("table2", "2"), false);
        }
    }

    [Test]
    [MethodDataSource(typeof(TestDataSources), nameof(TestDataSources.EqualityRecordIdCases))]
    public void ShouldBe(RecordId recordId1, RecordId? recordId2, bool expected)
    {
        bool result = recordId1 == recordId2!;

        result.Should().Be(expected);
    }
}
