namespace SurrealDb.Net.Tests.Models;

public class OperatorsStringRecordIdTests
{
    public static class TestDataSources
    {
        public static IEnumerable<Func<(StringRecordId, StringRecordId, bool)>> EqualityCases()
        {
            yield return () => ((StringRecordId)"table:id", (StringRecordId)"table:id", true);
            yield return () => ((StringRecordId)"table:one", (StringRecordId)"table:two", false);
        }
    }

    [Test]
    [MethodDataSource(typeof(TestDataSources), nameof(TestDataSources.EqualityCases))]
    public void ShouldBe(StringRecordId recordId1, StringRecordId recordId2, bool expected)
    {
        bool result = recordId1 == recordId2;

        result.Should().Be(expected);
    }
}
