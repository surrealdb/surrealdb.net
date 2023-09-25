namespace SurrealDb.Net.Tests.Models;

public class OperatorsThingTests
{
	private static readonly Thing ThingRef = new("table", "id");

	public static TheoryData<Thing, Thing?, bool> EqualityThingCases => new()
	{
		{ ThingRef, ThingRef, true },
		{ new Thing("table", "id"), null, false },
		{ new Thing("table", "id"), new Thing("table", "id"), true },
		{ new Thing("table", "1"), new Thing("table", "2"), false },
		{ new Thing("table1", "id"), new Thing("table2", "id"), false },
		{ new Thing("table1", "1"), new Thing("table2", "2"), false },
		{ new Thing("table", "⟨42⟩"), new Thing("table", "⟨42⟩"), true },
		{ new Thing("table", "⟨42⟩"), new Thing("table", "42"), false },
		{ new Thing("table", "⟨42⟩"), new Thing("table", "`42`"), true },
	};

	[Theory]
	[MemberData(nameof(EqualityThingCases))]
	public void ShouldBe(Thing thing1, Thing thing2, bool expected)
	{
		bool result = thing1 == thing2;

		result.Should().Be(expected);
	}
}
