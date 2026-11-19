using Dahomey.Cbor;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class MappingParent
{
    [CborProperty("child")]
    public MappingChild? Child { get; set; }
}

public class MappingChild
{
    [CborProperty("name")]
    public string? Name { get; set; }
}

public struct MappingPoint
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class MappingPointHolder
{
    [CborProperty("point")]
    public MappingPoint Point { get; set; }
}

public class MappingNullablePointHolder
{
    [CborProperty("point")]
    public MappingPoint? Point { get; set; }
}

public class MappingPricedRecord : SurrealDbRecord
{
    [CborProperty("price")]
    [CborIgnoreIfDefault]
    public decimal Price { get; set; }
}

/// <summary>
/// Pins how user models are read and written by Dahomey.Cbor's object mapper under the SurrealDB
/// CBOR options. These behaviours changed in Dahomey.Cbor 1.27/1.28, so a later upgrade that
/// changes them again fails here instead of reaching users unnoticed.
/// </summary>
public class ObjectMappingTests : BaseCborConverterTests
{
    [Test]
    public async Task EmptyMapShouldBeReadAsAnEmptyInstance()
    {
        // { "child": {} }
        var result = await DeserializeCborBinaryAsHexaAsync<MappingParent>("a1656368696c64a0");

        result.Child.Should().NotBeNull();
        result.Child!.Name.Should().BeNull();
    }

    [Test]
    public async Task NullIntoStructMemberShouldThrow()
    {
        // { "point": null }
        Func<Task> act = async () =>
            await DeserializeCborBinaryAsHexaAsync<MappingPointHolder>("a165706f696e74f6");

        await act.Should().ThrowAsync<CborException>().Where(e => e.Path == "$.point");
    }

    [Test]
    public async Task NullIntoNullableStructMemberShouldBeNull()
    {
        // { "point": null }
        var result = await DeserializeCborBinaryAsHexaAsync<MappingNullablePointHolder>(
            "a165706f696e74f6"
        );

        result.Point.Should().BeNull();
    }

    [Test]
    public async Task DuplicateMapKeyShouldThrow()
    {
        // { "name": "a", "name": "b" }
        Func<Task> act = async () =>
            await DeserializeCborBinaryAsHexaAsync<MappingChild>("a2646e616d656161646e616d656162");

        await act.Should().ThrowAsync<CborException>();
    }

    [Test]
    public async Task IgnoreIfDefaultShouldOmitTheDefaultValue()
    {
        string result = await SerializeCborBinaryAsHexaAsync(new MappingPricedRecord());

        // {} -- the null Record.Id and the 0m price are both omitted
        result.Should().Be("a0");
    }

    [Test]
    public async Task IgnoreIfDefaultShouldWriteAnEqualButNotIdenticalValue()
    {
        string result = await SerializeCborBinaryAsHexaAsync(
            new MappingPricedRecord { Price = 0.00m }
        );

        // { "price": 10("0.00") } -- 0.00m equals 0m but keeps its scale, so it is written
        result.Should().Be("a1657072696365ca64302e3030");
    }
}
