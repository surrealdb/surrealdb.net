namespace SurrealDb.Net.Tests.Models;

public class ObjectTableIdWithDateTime
{
    public string Location { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class ConstructorsRecordIdTests
{
    [Fact]
    public void ShouldCreateRecordIdUsingStringArguments()
    {
        var recordId = new RecordIdOfString("table", "id");

        recordId.Table.ToString().Should().Be("table");
        recordId.Id.ToString().Should().Be("id");
    }

    [Fact]
    public void ShouldCreateRecordIdUsingStringEscapedId()
    {
        var recordId = new RecordIdOfString("table", "⟨42⟩");

        recordId.Table.ToString().Should().Be("table");
        recordId.Id.ToString().Should().Be("⟨42⟩");
    }

    [Fact]
    public void ShouldCreateRecordIdUsingStringEscapedIdAlternative()
    {
        var recordId = new RecordIdOfString("table", "`42`");

        recordId.Table.ToString().Should().Be("table");
        recordId.Id.ToString().Should().Be("`42`");
    }

    [Fact]
    public void ShouldFailToCreateRecordIdFromNullId()
    {
        Action act = () => RecordId.From<string?>("table", null);

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Id should not be null (Parameter 'id')");
    }

    private static DateTime _januaryFirst2023 = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static TheoryData<object, Type, object> CreateRecordIdFromRecordIdCases =>
        new()
        {
            { "id", typeof(string), "id" },
            { "illeg@l_char$", typeof(string), "illeg@l_char$" },
            { "legal_chars", typeof(string), "legal_chars" },
            { "alsoL3ga1", typeof(string), "alsoL3ga1" },
            { "42", typeof(string), "42" },
            { 14, typeof(int), 14 },
            { 123456789012, typeof(long), 123456789012 },
            { (byte)8, typeof(byte), (byte)8 },
            { (short)9, typeof(short), (short)9 },
            { 'a', typeof(char), 'a' },
            {
                new Guid("8424486b-85b3-4448-ac8d-5d51083391c7"),
                typeof(Guid),
                new Guid("8424486b-85b3-4448-ac8d-5d51083391c7")
            },
            { (sbyte)8, typeof(sbyte), (sbyte)8 },
            { (ushort)9, typeof(ushort), (ushort)9 },
            { (uint)14, typeof(uint), (uint)14 },
            { (ulong)123456789012, typeof(ulong), (ulong)123456789012 },
            {
                new ObjectTableIdWithDateTime { Location = "London", Date = _januaryFirst2023 },
                typeof(ObjectTableIdWithDateTime),
                new ObjectTableIdWithDateTime { Location = "London", Date = _januaryFirst2023 }
            },
            {
                new List<object> { "London", _januaryFirst2023 },
                typeof(List<object>),
                new List<object> { "London", _januaryFirst2023 }
            },
        };

    [Theory]
    [MemberData(nameof(CreateRecordIdFromRecordIdCases))]
    public void CreateRecordIdFromRecordId(object id, Type type, object expected)
    {
        var recordId = RecordId.From("table", id);

        recordId.DeserializeId(type).Should().BeEquivalentTo(expected);
    }

    // 💡 bool, floating points (float, double, decimal, etc...), TimeSpan, DateTime are not valid record ids
}
