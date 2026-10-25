using Dahomey.Cbor;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class SizedNumberConverterTests : BaseCborConverterTests
{
    private const string U64_MAX_AS_DECIMAL = "ca743138343436373434303733373039353531363135";

    public static IEnumerable<Func<(SizedNumber, string)>> TaggedNumbers()
    {
        yield return () => (SizedNumber.I8(-128), "d8b4387f");
        yield return () => (SizedNumber.U8(255), "d8b518ff");
        yield return () => (SizedNumber.I16(-32768), "d8b6397fff");
        yield return () => (SizedNumber.U16(65535), "d8b719ffff");
        yield return () => (SizedNumber.I32(int.MinValue), "d8b83a7fffffff");
        yield return () => (SizedNumber.U32(uint.MaxValue), "d8b91affffffff");
        yield return () => (SizedNumber.U64(5), "d8ba05");
        yield return () => (SizedNumber.U64(ulong.MaxValue), "d8ba1bffffffffffffffff");
        yield return () => (SizedNumber.F16(1.5), "d8bbf93e00");
        yield return () => (SizedNumber.F16(65504), "d8bbf97bff");
        yield return () => (SizedNumber.F16(double.NaN), "d8bbf97e00");
        yield return () => (SizedNumber.F32(1.5f), "d8bcf93e00");
        yield return () => (SizedNumber.F32(0.1f), "d8bcfa3dcccccd");
        yield return () => (SizedNumber.F32(float.PositiveInfinity), "d8bcf97c00");
    }

    [Test]
    [MethodDataSource(nameof(TaggedNumbers))]
    public async Task SerializeTaggedOverCbor2(SizedNumber value, string expected)
    {
        string result = await SerializeCborBinaryAsHexaAsync(value, cbor2: true);

        result.Should().Be(expected);
    }

    [Test]
    [MethodDataSource(nameof(TaggedNumbers))]
    public async Task DeserializeTaggedOverCbor2(SizedNumber expected, string binary)
    {
        var result = await DeserializeCborBinaryAsHexaAsync<SizedNumber>(binary, cbor2: true);

        result.Should().Be(expected);
    }

    public static IEnumerable<Func<(SizedNumber, string)>> WidenedNumbers()
    {
        yield return () => (SizedNumber.I8(-128), "387f");
        yield return () => (SizedNumber.U8(255), "18ff");
        yield return () => (SizedNumber.U32(uint.MaxValue), "1affffffff");
        yield return () => (SizedNumber.U64(long.MaxValue), "1b7fffffffffffffff");
        yield return () => (SizedNumber.U64(ulong.MaxValue), U64_MAX_AS_DECIMAL);
        yield return () => (SizedNumber.F16(1.5), "f93e00");
        yield return () => (SizedNumber.F32(0.1f), "fa3dcccccd");
    }

    [Test]
    [MethodDataSource(nameof(WidenedNumbers))]
    public async Task SerializeWidenedOverPlainCbor(SizedNumber value, string expected)
    {
        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be(expected);
    }

    [Test]
    [Arguments("d8b505")]
    [Arguments("05")]
    public async Task ShouldNotDeserializeOverPlainCbor(string binary)
    {
        Func<Task> func = async () => await DeserializeCborBinaryAsHexaAsync<SizedNumber>(binary);

        await func.Should().ThrowAsync<CborException>();
    }

    [Test]
    [Arguments("d8b41880", "integer 128 is outside the 8-bit signed range")]
    [Arguments("d8b520", "integer -1 is outside the 8-bit unsigned range")]
    [Arguments("d8b91b0000000100000000", "integer 4294967296 is outside the 32-bit unsigned range")]
    [Arguments("d8ba20", "integer -1 is outside the 64-bit unsigned range")]
    [Arguments("d8bbfb3fb999999999999a", "float 0.1 cannot be losslessly represented as f16")]
    [Arguments("d8bcfb3fb999999999999a", "float 0.1 cannot be losslessly represented as f32")]
    [Arguments("d8bb01", "CBOR tag 187 needs a float")]
    [Arguments("d8b5f93c00", "CBOR tag 181 needs an integer")]
    [Arguments("d8b56131", "CBOR tag 181 needs an integer")]
    public async Task ShouldRejectOutOfRange(string binary, string message)
    {
        Func<Task> func = async () =>
            await DeserializeCborBinaryAsHexaAsync<SizedNumber>(binary, cbor2: true);

        await func.Should().ThrowAsync<CborException>().WithMessage(message);
    }

    [Test]
    public async Task ShouldKeepTheWidthInsideObjects()
    {
        const string binary = "a2616182d8b401d8b7026162d8bcf93e00";

        var result = await DeserializeCborBinaryAsHexaAsync<SizedNumbersObject>(
            binary,
            cbor2: true
        );

        result.A.Should().Equal(SizedNumber.I8(1), SizedNumber.U16(2));
        result.B.Should().Be(SizedNumber.F32(1.5f));

        string serialized = await SerializeCborBinaryAsHexaAsync(result, cbor2: true);
        serialized.Should().Be(binary);
    }

    [Test]
    public async Task ShouldNotTagPlainIntegers()
    {
        var value = new SizedNumberAndInteger { Int = 5, Sized = SizedNumber.U8(5) };

        string result = await SerializeCborBinaryAsHexaAsync(value, cbor2: true);

        result.Should().Be("a263696e74056573697a6564d8b505");
    }

    [Test]
    public void ShouldRefuseInexactHalf()
    {
        Action act = () => SizedNumber.F16(0.1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments(5.960464477539063e-8, true)]
    [Arguments(6.103515625e-05, true)]
    [Arguments(65504d, true)]
    [Arguments(65505d, false)]
    [Arguments(1.0009765625, true)]
    [Arguments(1.00048828125, false)]
    [Arguments(2.98023223876953125e-8, false)]
    public void ShouldTellWhichValuesFitInAHalf(double value, bool expected)
    {
        SizedNumber.FitsHalf(value).Should().Be(expected);
    }

    [Test]
    public void ShouldFormatAsSurrealQl()
    {
        SizedNumber.U8(5).ToString().Should().Be("5u8");
        SizedNumber.I32(-7).ToString().Should().Be("-7i32");
        SizedNumber.U64(ulong.MaxValue).ToString().Should().Be("18446744073709551615u64");
        SizedNumber.F32(1.5f).ToString().Should().Be("1.5f32");
        SizedNumber.F16(double.NegativeInfinity).ToString().Should().Be("<f16>-Infinity");
    }

    private sealed class SizedNumbersObject
    {
        [CborProperty("a")]
        public List<SizedNumber> A { get; set; } = [];

        [CborProperty("b")]
        public SizedNumber B { get; set; }
    }

    private sealed class SizedNumberAndInteger
    {
        [CborProperty("int")]
        public long Int { get; set; }

        [CborProperty("sized")]
        public SizedNumber Sized { get; set; }
    }

    [Test]
    public async Task ShouldReadNullableMembers()
    {
        var value = new NullableSizedNumbers { Value = SizedNumber.U16(7), Missing = null };

        string binary = await SerializeCborBinaryAsHexaAsync(value, cbor2: true);
        binary.Should().Be("a26576616c7565d8b707676d697373696e67f6");

        var result = await DeserializeCborBinaryAsHexaAsync<NullableSizedNumbers>(
            binary,
            cbor2: true
        );

        result.Value.Should().Be(SizedNumber.U16(7));
        result.Missing.Should().BeNull();
    }

    [Test]
    public async Task ShouldReadMembersOfRecords()
    {
        const string binary = "a2626964c88266706572736f6e016473697a65d8b505";

        var result = await DeserializeCborBinaryAsHexaAsync<SizedNumberRecord>(binary, cbor2: true);

        result.Id.Should().Be(new RecordIdOf<int>("person", 1));
        result.Size.Should().Be(SizedNumber.U8(5));
    }

    private sealed class NullableSizedNumbers
    {
        [CborProperty("value")]
        public SizedNumber? Value { get; set; }

        [CborProperty("missing")]
        public SizedNumber? Missing { get; set; }
    }

    private sealed class SizedNumberRecord : SurrealDbRecord
    {
        [CborProperty("size")]
        public SizedNumber Size { get; set; }
    }
}
