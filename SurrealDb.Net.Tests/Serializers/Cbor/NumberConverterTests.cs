using Dahomey.Cbor;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class NumberConverterTests : BaseCborConverterTests
{
    private const string U64_MAX_AS_DECIMAL = "ca743138343436373434303733373039353531363135";

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ShouldSerializeUInt64AboveInt64MaxAsDecimal(bool cbor2)
    {
        string result = await SerializeCborBinaryAsHexaAsync(ulong.MaxValue, cbor2);

        result.Should().Be(U64_MAX_AS_DECIMAL);
    }

    [Test]
    public async Task ShouldSerializeUInt64AsInteger()
    {
        string result = await SerializeCborBinaryAsHexaAsync((ulong)long.MaxValue);

        result.Should().Be("1b7fffffffffffffff");
    }

    [Test]
    [Arguments(U64_MAX_AS_DECIMAL)]
    [Arguments("d8ba1bffffffffffffffff")]
    [Arguments("1bffffffffffffffff")]
    public async Task ShouldDeserializeUInt64Max(string binary)
    {
        ulong result = await DeserializeCborBinaryAsHexaAsync<ulong>(binary, cbor2: true);

        result.Should().Be(ulong.MaxValue);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ShouldSerializeNativeNumbersWidened(bool cbor2)
    {
        (await SerializeCborBinaryAsHexaAsync((sbyte)-128, cbor2)).Should().Be("387f");
        (await SerializeCborBinaryAsHexaAsync((byte)255, cbor2)).Should().Be("18ff");
        (await SerializeCborBinaryAsHexaAsync((short)-32768, cbor2)).Should().Be("397fff");
        (await SerializeCborBinaryAsHexaAsync((ushort)65535, cbor2)).Should().Be("19ffff");
        (await SerializeCborBinaryAsHexaAsync(int.MinValue, cbor2)).Should().Be("3a7fffffff");
        (await SerializeCborBinaryAsHexaAsync(uint.MaxValue, cbor2)).Should().Be("1affffffff");
        (await SerializeCborBinaryAsHexaAsync(long.MinValue, cbor2))
            .Should()
            .Be("3b7fffffffffffffff");
        (await SerializeCborBinaryAsHexaAsync(1.5f, cbor2)).Should().Be("f93e00");
        (await SerializeCborBinaryAsHexaAsync(0.1f, cbor2)).Should().Be("fa3dcccccd");
        (await SerializeCborBinaryAsHexaAsync(0.1d, cbor2)).Should().Be("fb3fb999999999999a");
        (await SerializeCborBinaryAsHexaAsync((Half)1.5, cbor2)).Should().Be("f93e00");
    }

    [Test]
    public async Task ShouldDeserializeSizedNumbersIntoNativeNumbers()
    {
        (await DeserializeCborBinaryAsHexaAsync<sbyte>("d8b4387f", cbor2: true)).Should().Be(-128);
        (await DeserializeCborBinaryAsHexaAsync<byte>("d8b518ff", cbor2: true)).Should().Be(255);
        (await DeserializeCborBinaryAsHexaAsync<int>("d8b505", cbor2: true)).Should().Be(5);
        (await DeserializeCborBinaryAsHexaAsync<long>("d8b83a7fffffff", cbor2: true))
            .Should()
            .Be(int.MinValue);
        (await DeserializeCborBinaryAsHexaAsync<uint>("d8b91affffffff", cbor2: true))
            .Should()
            .Be(uint.MaxValue);
        (await DeserializeCborBinaryAsHexaAsync<float>("d8bcfa3dcccccd", cbor2: true))
            .Should()
            .Be(0.1f);
        (await DeserializeCborBinaryAsHexaAsync<double>("d8bbf93e00", cbor2: true))
            .Should()
            .Be(1.5);
        (await DeserializeCborBinaryAsHexaAsync<Half>("d8bbf93e00", cbor2: true))
            .Should()
            .Be((Half)1.5);
        (await DeserializeCborBinaryAsHexaAsync<int?>("d8b505", cbor2: true)).Should().Be(5);
    }

    [Test]
    [Arguments("d8b41880")]
    [Arguments("d8bb01")]
    [Arguments("d8ba1bffffffffffffffff")]
    [Arguments("ca63312e35")]
    public async Task ShouldRefuseNumbersThatDoNotFitInt64(string binary)
    {
        Func<Task> func = async () =>
            await DeserializeCborBinaryAsHexaAsync<long>(binary, cbor2: true);

        await func.Should().ThrowAsync<CborException>();
    }

    [Test]
    public async Task ShouldRefuseSizedNumbersThatDoNotFitTheType()
    {
        Func<Task> func = async () =>
            await DeserializeCborBinaryAsHexaAsync<byte>("d8b719ffff", cbor2: true);

        await func.Should().ThrowAsync<CborException>();
    }

    [Test]
    public async Task ShouldRoundTripUInt64AboveInt64MaxInObjects()
    {
        var value = new UInt64Object { Value = ulong.MaxValue, Nullable = ulong.MaxValue };

        string binary = await SerializeCborBinaryAsHexaAsync(value);
        var result = await DeserializeCborBinaryAsHexaAsync<UInt64Object>(binary);

        result.Value.Should().Be(ulong.MaxValue);
        result.Nullable.Should().Be(ulong.MaxValue);
    }

    private sealed class UInt64Object
    {
        [CborProperty("value")]
        public ulong Value { get; set; }

        [CborProperty("nullable")]
        public ulong? Nullable { get; set; }
    }
}
