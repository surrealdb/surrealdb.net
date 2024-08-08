namespace SurrealDb.Net.Tests.Models;

public class ObjectTableIdWithDateTime
{
    public string Location { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class ConstructorsRecordIdTests
{
    [Fact]
    public void ShouldCreateRecordIdUsingTwoArguments()
    {
        var recordId = new RecordId("table", "id");

        recordId.Table.ToString().Should().Be("table");
        recordId.Id.ToString().Should().Be("id");
        recordId.ToString().Should().Be("table:id");
    }

    [Fact]
    public void ShouldCreateRecordIdEscapedUsingTwoArguments()
    {
        var recordId = new RecordId("table", "⟨42⟩");

        recordId.Table.ToString().Should().Be("table");
        recordId.UnescapedTable.ToString().Should().Be("table");
        recordId.Id.ToString().Should().Be("⟨42⟩");
        recordId.UnescapedId.ToString().Should().Be("42");
        recordId.ToString().Should().Be("table:⟨42⟩");
    }

    [Fact]
    public void ShouldCreateRecordIdEscapedAlternativeUsingTwoArguments()
    {
        var recordId = new RecordId("table", "`42`");

        recordId.Table.ToString().Should().Be("table");
        recordId.UnescapedTable.ToString().Should().Be("table");
        recordId.Id.ToString().Should().Be("`42`");
        recordId.UnescapedId.ToString().Should().Be("42");
        recordId.ToString().Should().Be("table:`42`");
    }

    [Fact]
    public void ShouldCreateRecordIdUsingOneArgument()
    {
        var recordId = new RecordId("table:id");

        recordId.Table.ToString().Should().Be("table");
        recordId.Id.ToString().Should().Be("id");
        recordId.ToString().Should().Be("table:id");
    }

    [Fact]
    public void ShouldCreateRecordIdWithEscapedIdUsingOneArgument()
    {
        var recordId = new RecordId("table:⟨42⟩");

        recordId.Table.ToString().Should().Be("table");
        recordId.UnescapedTable.ToString().Should().Be("table");
        recordId.Id.ToString().Should().Be("⟨42⟩");
        recordId.UnescapedId.ToString().Should().Be("42");
        recordId.ToString().Should().Be("table:⟨42⟩");
    }

    [Fact]
    public void ShouldCreateRecordIdWithEscapedIdAlternativeUsingOneArgument()
    {
        var recordId = new RecordId("table:`42`");

        recordId.Table.ToString().Should().Be("table");
        recordId.UnescapedTable.ToString().Should().Be("table");
        recordId.Id.ToString().Should().Be("`42`");
        recordId.UnescapedId.ToString().Should().Be("42");
        recordId.ToString().Should().Be("table:`42`");
    }

    [Fact]
    public void ShouldCreateRecordIdUsingTwoEscapedArguments()
    {
        var recordId = new RecordId("⟨42⟩", "⟨42⟩");

        recordId.Table.ToString().Should().Be("⟨42⟩");
        recordId.UnescapedTable.ToString().Should().Be("42");
        recordId.Id.ToString().Should().Be("⟨42⟩");
        recordId.UnescapedId.ToString().Should().Be("42");
        recordId.ToString().Should().Be("⟨42⟩:⟨42⟩");
    }

    [Fact]
    public void ShouldCreateRecordIdAlternativeUsingTwoEscapedArguments()
    {
        var recordId = new RecordId("`42`", "`42`");

        recordId.Table.ToString().Should().Be("`42`");
        recordId.UnescapedTable.ToString().Should().Be("42");
        recordId.Id.ToString().Should().Be("`42`");
        recordId.UnescapedId.ToString().Should().Be("42");
        recordId.ToString().Should().Be("`42`:`42`");
    }

    [Fact]
    public void ShouldCreateRecordIdWithEscapedTableUsingOneArgument()
    {
        var recordId = new RecordId("⟨42⟩:id");

        recordId.Table.ToString().Should().Be("⟨42⟩");
        recordId.UnescapedTable.ToString().Should().Be("42");
        recordId.Id.ToString().Should().Be("id");
        recordId.UnescapedId.ToString().Should().Be("id");
        recordId.ToString().Should().Be("⟨42⟩:id");
    }

    [Fact]
    public void ShouldCreateRecordIdWithEscapedTableAlternativeUsingOneArgument()
    {
        var recordId = new RecordId("`42`:id");

        recordId.Table.ToString().Should().Be("`42`");
        recordId.UnescapedTable.ToString().Should().Be("42");
        recordId.Id.ToString().Should().Be("id");
        recordId.UnescapedId.ToString().Should().Be("id");
        recordId.ToString().Should().Be("`42`:id");
    }

    [Fact]
    public void ShouldFailToCreateRecordIdIfNoSeparator()
    {
        Action act = () => new RecordId("just_id");

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Cannot detect separator on RecordId (Parameter 'recordId')");
    }

    [Fact]
    public void ShouldFailToCreateRecordIdFromNullTable()
    {
        Action act = () => RecordId.From<string?, string>(null, "id");

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Table should not be null (Parameter 'table')");
    }

    [Fact]
    public void ShouldFailToCreateRecordIdFromNullId()
    {
        Action act = () => RecordId.From<string, string?>("table", null);

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Id should not be null (Parameter 'id')");
    }

    [Fact]
    public void ShouldCreateRecordIdWithColonInEscapedTable()
    {
        var recordId = new RecordId("⟨https://surrealdb.com/⟩:id");

        recordId.Table.ToString().Should().Be("⟨https://surrealdb.com/⟩");
        recordId.UnescapedTable.ToString().Should().Be("https://surrealdb.com/");
        recordId.Id.ToString().Should().Be("id");
        recordId.UnescapedId.ToString().Should().Be("id");
        recordId.ToString().Should().Be("⟨https://surrealdb.com/⟩:id");
    }

    [Fact]
    public void ShouldCreateRecordIdWithColonInAlternativeEscapedTable()
    {
        var recordId = new RecordId("`https://surrealdb.com/`:id");

        recordId.Table.ToString().Should().Be("`https://surrealdb.com/`");
        recordId.UnescapedTable.ToString().Should().Be("https://surrealdb.com/");
        recordId.Id.ToString().Should().Be("id");
        recordId.UnescapedId.ToString().Should().Be("id");
        recordId.ToString().Should().Be("`https://surrealdb.com/`:id");
    }

    [Fact]
    public void ShouldCreateRecordIdWithColonInEscapedId()
    {
        var recordId = new RecordId("table:⟨https://surrealdb.com/⟩");

        recordId.Table.ToString().Should().Be("table");
        recordId.UnescapedTable.ToString().Should().Be("table");
        recordId.Id.ToString().Should().Be("⟨https://surrealdb.com/⟩");
        recordId.UnescapedId.ToString().Should().Be("https://surrealdb.com/");
        recordId.ToString().Should().Be("table:⟨https://surrealdb.com/⟩");
    }

    [Fact]
    public void ShouldCreateRecordIdWithColonInAlternativeEscapedId()
    {
        var recordId = new RecordId("table:`https://surrealdb.com/`");

        recordId.Table.ToString().Should().Be("table");
        recordId.UnescapedTable.ToString().Should().Be("table");
        recordId.Id.ToString().Should().Be("`https://surrealdb.com/`");
        recordId.UnescapedId.ToString().Should().Be("https://surrealdb.com/");
        recordId.ToString().Should().Be("table:`https://surrealdb.com/`");
    }

    private static DateTime _januaryFirst2023 = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static TheoryData<object, string> CreateRecordIdFromTableCases =>
        new()
        {
            { "table", "table:id" },
            { "illeg@l_char$", "⟨illeg@l_char$⟩:id" },
            { "legal_chars", "legal_chars:id" },
            { "alsoL3ga1", "alsoL3ga1:id" },
            { "42", "⟨42⟩:id" },
            { 14, "14:id" },
            { 123456789012, "123456789012:id" },
            { (byte)8, "8:id" },
            { (short)9, "9:id" },
            { 'a', "a:id" },
            {
                new Guid("8424486b-85b3-4448-ac8d-5d51083391c7"),
                "⟨8424486b-85b3-4448-ac8d-5d51083391c7⟩:id"
            },
            { (sbyte)8, "8:id" },
            { (ushort)9, "9:id" },
            { (uint)14, "14:id" },
            { (ulong)123456789012, "123456789012:id" },
            {
                new ObjectTableIdWithDateTime { Location = "London", Date = _januaryFirst2023 },
                "{\"location\":\"London\",\"date\":\"2023-01-01T00:00:00.0000000Z\"}:id"
            },
            {
                new List<object> { "London", _januaryFirst2023 },
                "[\"London\",\"2023-01-01T00:00:00.0000000Z\"]:id"
            },
        };

    [Theory(Skip = "Rewrite tests with updated RecordId type")]
    [MemberData(nameof(CreateRecordIdFromTableCases))]
    public void CreateRecordIdFromTable(object table, string expected)
    {
        var recordId = RecordId.From(table, "id");

        recordId.ToString().Should().Be(expected);
    }

    public static TheoryData<object, string> CreateRecordIdFromRecordIdCases =>
        new()
        {
            { "id", "table:id" },
            { "illeg@l_char$", "table:⟨illeg@l_char$⟩" },
            { "legal_chars", "table:legal_chars" },
            { "alsoL3ga1", "table:alsoL3ga1" },
            { "42", "table:⟨42⟩" },
            { 14, "table:14" },
            { 123456789012, "table:123456789012" },
            { (byte)8, "table:8" },
            { (short)9, "table:9" },
            { 'a', "table:a" },
            {
                new Guid("8424486b-85b3-4448-ac8d-5d51083391c7"),
                "table:⟨8424486b-85b3-4448-ac8d-5d51083391c7⟩"
            },
            { (sbyte)8, "table:8" },
            { (ushort)9, "table:9" },
            { (uint)14, "table:14" },
            { (ulong)123456789012, "table:123456789012" },
            {
                new ObjectTableIdWithDateTime { Location = "London", Date = _januaryFirst2023 },
                "table:{\"location\":\"London\",\"date\":\"2023-01-01T00:00:00.0000000Z\"}"
            },
            {
                new List<object> { "London", _januaryFirst2023 },
                "table:[\"London\",\"2023-01-01T00:00:00.0000000Z\"]"
            },
        };

    [Theory(Skip = "Rewrite tests with updated RecordId type")]
    [MemberData(nameof(CreateRecordIdFromRecordIdCases))]
    public void CreateRecordIdFromRecordId(object id, string expected)
    {
        var recordId = RecordId.From("table", id);

        recordId.ToString().Should().Be(expected);
    }

    // 💡 bool, floating points (float, double, decimal, etc...), TimeSpan, DateTime are not valid record ids
}
