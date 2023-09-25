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
		thing.Id.ToString().Should().Be("⟨42⟩");
		thing.UnescapedId.ToString().Should().Be("42");
		thing.ToString().Should().Be("table:⟨42⟩");
	}

	[Fact]
	public void ShouldCreateThingEscapedAlternativeUsingTwoArguments()
	{
		var thing = new Thing("table", "`42`");

		thing.Table.ToString().Should().Be("table");
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
	public void ShouldCreateThingEscapedUsingOneArgument()
	{
		var thing = new Thing("table:⟨42⟩");

		thing.Table.ToString().Should().Be("table");
		thing.Id.ToString().Should().Be("⟨42⟩");
		thing.UnescapedId.ToString().Should().Be("42");
		thing.ToString().Should().Be("table:⟨42⟩");
	}

	[Fact]
	public void ShouldCreateThingEscapedAlternativeUsingOneArgument()
	{
		var thing = new Thing("table:`42`");

		thing.Table.ToString().Should().Be("table");
		thing.Id.ToString().Should().Be("`42`");
		thing.UnescapedId.ToString().Should().Be("42");
		thing.ToString().Should().Be("table:`42`");
	}

	[Fact]
	public void ShouldFailToCreateThingIfNoSeparator()
	{
		Action act = () => new Thing("just_id");

		act.Should().Throw<ArgumentException>().WithMessage("Cannot detect separator on Thing (Parameter 'thing')");
	}

	[Fact]
	public void ShouldFailToCreateThingFromNullId()
	{
		Action act = () => Thing.From<string?>("table", null);

		act.Should().Throw<ArgumentException>().WithMessage("Id should not be null (Parameter 'id')");
	}

	private static DateTime _januaryFirst2023 = new(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public static TheoryData<object, string> CreateThingFromCases => new()
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
		{ new Guid("8424486b-85b3-4448-ac8d-5d51083391c7"), "table:⟨8424486b-85b3-4448-ac8d-5d51083391c7⟩" },
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
	[MemberData(nameof(CreateThingFromCases))]
	public void CreateThingFrom(object id, string expected)
	{
		var thing = Thing.From("table", id);

		thing.ToString().Should().Be(expected);
	}

	// bool, floating points (float, double, decimal, etc...), TimeSpan, DateTime are not valid record ids
}
