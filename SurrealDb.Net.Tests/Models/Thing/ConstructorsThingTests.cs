using System.Text.Json;

namespace SurrealDb.Net.Tests.Models;

public class ObjectTableIdWithDateTime
{
    public string Location { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class ConstructorsThingTests
{
    [Fact]
    public void ShouldCreateThingUsingTwoArguments()
    {
        var thing = new Thing("table", "id");

        thing.Table.ToString().Should().Be("table");
        thing.Id.ToString().Should().Be("id");
        thing.ToString().Should().Be("table:id");
    }

    [Fact]
    public void ShouldCreateThingEscapedUsingTwoArguments()
    {
        var thing = new Thing("table", "⟨42⟩");

        thing.Table.ToString().Should().Be("table");
        thing.UnescapedTable.ToString().Should().Be("table");
        thing.Id.ToString().Should().Be("⟨42⟩");
        thing.UnescapedId.ToString().Should().Be("42");
        thing.ToString().Should().Be("table:⟨42⟩");
    }

    [Fact]
    public void ShouldCreateThingEscapedAlternativeUsingTwoArguments()
    {
        var thing = new Thing("table", "`42`");

        thing.Table.ToString().Should().Be("table");
        thing.UnescapedTable.ToString().Should().Be("table");
        thing.Id.ToString().Should().Be("`42`");
        thing.UnescapedId.ToString().Should().Be("42");
        thing.ToString().Should().Be("table:`42`");
    }

    [Fact]
    public void ShouldCreateThingUsingOneArgument()
    {
        var thing = new Thing("table:id");

        thing.Table.ToString().Should().Be("table");
        thing.Id.ToString().Should().Be("id");
        thing.ToString().Should().Be("table:id");
    }

    [Fact]
    public void ShouldCreateThingWithEscapedIdUsingOneArgument()
    {
        var thing = new Thing("table:⟨42⟩");

        thing.Table.ToString().Should().Be("table");
        thing.UnescapedTable.ToString().Should().Be("table");
        thing.Id.ToString().Should().Be("⟨42⟩");
        thing.UnescapedId.ToString().Should().Be("42");
        thing.ToString().Should().Be("table:⟨42⟩");
    }

    [Fact]
    public void ShouldCreateThingWithEscapedIdAlternativeUsingOneArgument()
    {
        var thing = new Thing("table:`42`");

        thing.Table.ToString().Should().Be("table");
        thing.UnescapedTable.ToString().Should().Be("table");
        thing.Id.ToString().Should().Be("`42`");
        thing.UnescapedId.ToString().Should().Be("42");
        thing.ToString().Should().Be("table:`42`");
    }

    [Fact]
    public void ShouldCreateThingUsingTwoEscapedArguments()
    {
        var thing = new Thing("⟨42⟩", "⟨42⟩");

        thing.Table.ToString().Should().Be("⟨42⟩");
        thing.UnescapedTable.ToString().Should().Be("42");
        thing.Id.ToString().Should().Be("⟨42⟩");
        thing.UnescapedId.ToString().Should().Be("42");
        thing.ToString().Should().Be("⟨42⟩:⟨42⟩");
    }

    [Fact]
    public void ShouldCreateThingAlternativeUsingTwoEscapedArguments()
    {
        var thing = new Thing("`42`", "`42`");

        thing.Table.ToString().Should().Be("`42`");
        thing.UnescapedTable.ToString().Should().Be("42");
        thing.Id.ToString().Should().Be("`42`");
        thing.UnescapedId.ToString().Should().Be("42");
        thing.ToString().Should().Be("`42`:`42`");
    }

    [Fact]
    public void ShouldCreateThingWithEscapedTableUsingOneArgument()
    {
        var thing = new Thing("⟨42⟩:id");

        thing.Table.ToString().Should().Be("⟨42⟩");
        thing.UnescapedTable.ToString().Should().Be("42");
        thing.Id.ToString().Should().Be("id");
        thing.UnescapedId.ToString().Should().Be("id");
        thing.ToString().Should().Be("⟨42⟩:id");
    }

    [Fact]
    public void ShouldCreateThingWithEscapedTableAlternativeUsingOneArgument()
    {
        var thing = new Thing("`42`:id");

        thing.Table.ToString().Should().Be("`42`");
        thing.UnescapedTable.ToString().Should().Be("42");
        thing.Id.ToString().Should().Be("id");
        thing.UnescapedId.ToString().Should().Be("id");
        thing.ToString().Should().Be("`42`:id");
    }

    [Fact]
    public void ShouldFailToCreateThingIfNoSeparator()
    {
        Action act = () => new Thing("just_id");

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Cannot detect separator on Thing (Parameter 'thing')");
    }

    [Fact]
    public void ShouldFailToCreateThingFromNullTable()
    {
        Action act = () => Thing.From<string?, string>(null, "id");

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Table should not be null (Parameter 'table')");
    }

    [Fact]
    public void ShouldFailToCreateThingFromNullId()
    {
        Action act = () => Thing.From<string, string?>("table", null);

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Id should not be null (Parameter 'id')");
    }

    [Fact]
    public void ShouldCreateThingWithColonInEscapedTable()
    {
        var thing = new Thing("⟨https://surrealdb.com/⟩:id");

        thing.Table.ToString().Should().Be("⟨https://surrealdb.com/⟩");
        thing.UnescapedTable.ToString().Should().Be("https://surrealdb.com/");
        thing.Id.ToString().Should().Be("id");
        thing.UnescapedId.ToString().Should().Be("id");
        thing.ToString().Should().Be("⟨https://surrealdb.com/⟩:id");
    }

    [Fact]
    public void ShouldCreateThingWithColonInAlternativeEscapedTable()
    {
        var thing = new Thing("`https://surrealdb.com/`:id");

        thing.Table.ToString().Should().Be("`https://surrealdb.com/`");
        thing.UnescapedTable.ToString().Should().Be("https://surrealdb.com/");
        thing.Id.ToString().Should().Be("id");
        thing.UnescapedId.ToString().Should().Be("id");
        thing.ToString().Should().Be("`https://surrealdb.com/`:id");
    }

    [Fact]
    public void ShouldCreateThingWithColonInEscapedId()
    {
        var thing = new Thing("table:⟨https://surrealdb.com/⟩");

        thing.Table.ToString().Should().Be("table");
        thing.UnescapedTable.ToString().Should().Be("table");
        thing.Id.ToString().Should().Be("⟨https://surrealdb.com/⟩");
        thing.UnescapedId.ToString().Should().Be("https://surrealdb.com/");
        thing.ToString().Should().Be("table:⟨https://surrealdb.com/⟩");
    }

    [Fact]
    public void ShouldCreateThingWithColonInAlternativeEscapedId()
    {
        var thing = new Thing("table:`https://surrealdb.com/`");

        thing.Table.ToString().Should().Be("table");
        thing.UnescapedTable.ToString().Should().Be("table");
        thing.Id.ToString().Should().Be("`https://surrealdb.com/`");
        thing.UnescapedId.ToString().Should().Be("https://surrealdb.com/");
        thing.ToString().Should().Be("table:`https://surrealdb.com/`");
    }

    private static DateTime _januaryFirst2023 = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static TheoryData<object, string> CreateThingFromTableCases =>
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

    [Theory]
    [MemberData(nameof(CreateThingFromTableCases))]
    public void CreateThingFromTable(object table, string expected)
    {
        var thing = Thing.From(table, "id", JsonNamingPolicy.SnakeCaseLower);

        thing.ToString().Should().Be(expected);
    }

    public static TheoryData<object, string> CreateThingFromRecordIdCases =>
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

    [Theory]
    [MemberData(nameof(CreateThingFromRecordIdCases))]
    public void CreateThingFromRecordId(object id, string expected)
    {
        var thing = Thing.From("table", id, JsonNamingPolicy.SnakeCaseLower);

        thing.ToString().Should().Be(expected);
    }

    // 💡 bool, floating points (float, double, decimal, etc...), TimeSpan, DateTime are not valid record ids
}
