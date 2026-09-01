using SurrealDb.Net.Tests.Serializers.Cbor;

namespace SurrealDb.Net.Tests.Models;

public class EqualityRecordIdTests : BaseCborConverterTests
{
    private const string ArrayRecordId = "c88264706f737482664c6f6e646f6e655061726973";

    [Test]
    public async Task ShouldUseValueEqualityForDeserializedComplexIds()
    {
        var first = await DeserializeCborBinaryAsHexaAsync<RecordId>(ArrayRecordId);
        var second = await DeserializeCborBinaryAsHexaAsync<RecordId>(ArrayRecordId);

        AssertEqualityContract(first, second);
        ReferenceEquals(first, second).Should().BeFalse();
    }

    [Test]
    public async Task ShouldFindEquivalentDeserializedIdInHashCollections()
    {
        var stored = await DeserializeCborBinaryAsHexaAsync<RecordId>(ArrayRecordId);
        var lookup = await DeserializeCborBinaryAsHexaAsync<RecordId>(ArrayRecordId);
        var set = new HashSet<RecordId> { stored };
        var dictionary = new Dictionary<RecordId, string> { [stored] = "value" };

        set.Should().Contain(lookup);
        dictionary.Should().ContainKey(lookup).WhoseValue.Should().Be("value");
    }

    [Test]
    public async Task ShouldBeSymmetricAcrossGenericAndDeserializedIds()
    {
        var first = new RecordIdOf<string[]>("post", ["London", "Paris"]);
        var second = await DeserializeCborBinaryAsHexaAsync<RecordId>(ArrayRecordId);
        var third = new RecordIdOf<List<string>>("post", ["London", "Paris"]);

        AssertEqualityContract(first, second);
        AssertEqualityContract(second, third);
        AssertEqualityContract(first, third);
    }

    [Test]
    public void ShouldUseStructuralEqualityForObjectAndArrayIds()
    {
        var firstObject = new RecordIdOf<Dictionary<string, string>>(
            "place",
            new Dictionary<string, string> { ["city"] = "London" }
        );
        var secondObject = new RecordIdOf<Dictionary<string, string>>(
            "place",
            new Dictionary<string, string> { ["city"] = "London" }
        );
        var firstArray = new RecordIdOf<string[]>("place", ["London", "Paris"]);
        var secondArray = new RecordIdOf<List<string>>("place", ["London", "Paris"]);

        AssertEqualityContract(firstObject, secondObject);
        AssertEqualityContract(firstArray, secondArray);
    }

    [Test]
    public void ShouldTreatEquivalentIntegerRepresentationsAsEqual()
    {
        var intId = new RecordIdOf<int>("post", 42);
        var longId = new RecordIdOf<long>("post", 42L);
        var unsignedId = new RecordIdOf<uint>("post", 42U);

        AssertEqualityContract(intId, longId);
        AssertEqualityContract(longId, unsignedId);
        AssertEqualityContract(intId, unsignedId);
    }

    [Test]
    public void ShouldRemainUnequalForDifferentTableOrId()
    {
        var expected = new RecordIdOfString("post", "first");
        var differentTable = new RecordIdOfString("Post", "first");
        var differentId = new RecordIdOfString("post", "second");

        expected.Equals(differentTable).Should().BeFalse();
        expected.Equals(differentId).Should().BeFalse();
        (expected == differentTable).Should().BeFalse();
        (expected != differentId).Should().BeTrue();
    }

    private static void AssertEqualityContract(RecordId first, RecordId second)
    {
        first.Equals(first).Should().BeTrue();
        first.Equals(second).Should().BeTrue();
        second.Equals(first).Should().BeTrue();
        first.Equals((object)second).Should().BeTrue();
        (first == second).Should().BeTrue();
        (first != second).Should().BeFalse();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}
