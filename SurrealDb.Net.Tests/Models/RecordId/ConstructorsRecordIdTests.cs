namespace SurrealDb.Net.Tests.Models;

public class ObjectTableIdWithDateTime
{
    public string Location { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class ConstructorsRecordIdTests
{
    [Test]
    public void ShouldCreateRecordIdUsingStringArguments()
    {
        var recordId = new RecordIdOfString("table", "id");

        recordId.Table.ToString().Should().Be("table");
        recordId.Id.ToString().Should().Be("id");
    }

    [Test]
    public void ShouldCreateRecordIdUsingStringEscapedId()
    {
        var recordId = new RecordIdOfString("table", "⟨42⟩");

        recordId.Table.ToString().Should().Be("table");
        recordId.Id.ToString().Should().Be("⟨42⟩");
    }

    [Test]
    public void ShouldCreateRecordIdUsingStringEscapedIdAlternative()
    {
        var recordId = new RecordIdOfString("table", "`42`");

        recordId.Table.ToString().Should().Be("table");
        recordId.Id.ToString().Should().Be("`42`");
    }

    [Test]
    public void ShouldFailToCreateRecordIdFromNullId()
    {
        Action act = () => RecordId.From<string?>("table", null);

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Id should not be null (Parameter 'id')");
    }

    private static DateTime _januaryFirst2023 = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static class TestDataSources
    {
        public static IEnumerable<Func<(object, Type, object)>> CreateRecordIdFromRecordIdCases()
        {
            yield return () => ("id", typeof(string), "id");
            yield return () => ("illeg@l_char$", typeof(string), "illeg@l_char$");
            yield return () => ("legal_chars", typeof(string), "legal_chars");
            yield return () => ("alsoL3ga1", typeof(string), "alsoL3ga1");
            yield return () => ("42", typeof(string), "42");
            yield return () => (14, typeof(int), 14);
            yield return () => (123456789012, typeof(long), 123456789012);
            yield return () => ((byte)8, typeof(byte), (byte)8);
            yield return () => ((short)9, typeof(short), (short)9);
            yield return () => ('a', typeof(char), 'a');
            yield return () =>
                (
                    new Guid("8424486b-85b3-4448-ac8d-5d51083391c7"),
                    typeof(Guid),
                    new Guid("8424486b-85b3-4448-ac8d-5d51083391c7")
                );
            yield return () => ((sbyte)8, typeof(sbyte), (sbyte)8);
            yield return () => ((ushort)9, typeof(ushort), (ushort)9);
            yield return () => ((uint)14, typeof(uint), (uint)14);
            yield return () => ((ulong)123456789012, typeof(ulong), (ulong)123456789012);
            yield return () =>
                (
                    new ObjectTableIdWithDateTime { Location = "London", Date = _januaryFirst2023 },
                    typeof(ObjectTableIdWithDateTime),
                    new ObjectTableIdWithDateTime { Location = "London", Date = _januaryFirst2023 }
                );
            yield return () =>
                (
                    new List<object> { "London", _januaryFirst2023 },
                    typeof(List<object>),
                    new List<object> { "London", _januaryFirst2023 }
                );
        }
    }

    [Test]
    [MethodDataSource(
        typeof(TestDataSources),
        nameof(TestDataSources.CreateRecordIdFromRecordIdCases)
    )]
    public void CreateRecordIdFromRecordId(object id, Type type, object expected)
    {
        var recordId = RecordId.From("table", id);

        recordId.DeserializeId(type).Should().BeEquivalentTo(expected);
    }

    // 💡 bool, floating points (float, double, decimal, etc...), TimeSpan, DateTime are not valid record ids
}
