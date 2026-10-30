using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Extensions;

namespace SurrealDb.Net.Tests;

public class Cbor2Tests
{
    public static IEnumerable<Func<SizedNumber>> SizedNumbers()
    {
        yield return () => SizedNumber.I8(sbyte.MinValue);
        yield return () => SizedNumber.U8(byte.MaxValue);
        yield return () => SizedNumber.I16(short.MinValue);
        yield return () => SizedNumber.U16(ushort.MaxValue);
        yield return () => SizedNumber.I32(int.MinValue);
        yield return () => SizedNumber.U32(uint.MaxValue);
        yield return () => SizedNumber.U64(ulong.MaxValue);
        yield return () => SizedNumber.F16(65504);
        yield return () => SizedNumber.F32(0.1f);
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ShouldNegotiateCbor2FromSurrealDb4(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        await using var client = surrealDbClientGenerator.Create(connectionString);

        var version = (await client.Version()).ToSemver();
        bool negotiated = await RequiresCbor2Attribute.IsNegotiatedAsync(client);

        negotiated.Should().Be(version.Major >= 4);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [RequiresCbor2]
    public async Task ShouldRoundTripSizedNumbers(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        await using var client = surrealDbClientGenerator.Create(connectionString);

        foreach (var getValue in SizedNumbers())
        {
            var value = getValue();

            var response = await client.Query($"RETURN {value};");

            response.GetValue<SizedNumber>(0).Should().Be(value);
        }
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [RequiresCbor2]
    public async Task ShouldReadSizedNumbers(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        await using var client = surrealDbClientGenerator.Create(connectionString);

        var response = await client.RawQuery(
            "RETURN { size: 200u8, delta: -5i16, ratio: 1.5f32, big: 18446744073709551615u64 };"
        );

        var sized = response.GetValue<SizedNumbersModel>(0)!;
        sized.Size.Should().Be(SizedNumber.U8(200));
        sized.Delta.Should().Be(SizedNumber.I16(-5));
        sized.Ratio.Should().Be(SizedNumber.F32(1.5f));
        sized.Big.Should().Be(SizedNumber.U64(ulong.MaxValue));

        var native = response.GetValue<NativeNumbersModel>(0)!;
        native.Size.Should().Be(200);
        native.Delta.Should().Be(-5);
        native.Ratio.Should().Be(1.5f);
        native.Big.Should().Be(ulong.MaxValue);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSendSizedNumbersToAnyServer(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        await using var client = surrealDbClientGenerator.Create(connectionString);

        foreach (var getValue in SizedNumbers())
        {
            var value = getValue();

            var response = await client.Query($"RETURN {value};");

            if (value.IsFloat)
            {
                response.GetValue<double>(0).Should().Be(value.ToDouble());
            }
            else if (value.Kind == SizedNumberKind.U64)
            {
                response.GetValue<ulong>(0).Should().Be(value.ToUInt64());
            }
            else
            {
                response.GetValue<long>(0).Should().Be(value.ToInt64());
            }
        }
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldRoundTripUInt64AboveInt64Max(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        var response = await client.Query($"RETURN {ulong.MaxValue};");
        response.GetValue<ulong>(0).Should().Be(ulong.MaxValue);

        var created = await client.Create(
            "counter",
            new Counter { Value = ulong.MaxValue, Nullable = (ulong)long.MaxValue + 1 }
        );
        var selected = await client.Select<Counter>(created.Id!);

        selected!.Value.Should().Be(ulong.MaxValue);
        selected.Nullable.Should().Be((ulong)long.MaxValue + 1);
    }

    private sealed class SizedNumbersModel
    {
        [CborProperty("size")]
        public SizedNumber Size { get; set; }

        [CborProperty("delta")]
        public SizedNumber Delta { get; set; }

        [CborProperty("ratio")]
        public SizedNumber? Ratio { get; set; }

        [CborProperty("big")]
        public SizedNumber Big { get; set; }
    }

    private sealed class NativeNumbersModel
    {
        [CborProperty("size")]
        public byte Size { get; set; }

        [CborProperty("delta")]
        public int Delta { get; set; }

        [CborProperty("ratio")]
        public float Ratio { get; set; }

        [CborProperty("big")]
        public ulong Big { get; set; }
    }

    private sealed class Counter : SurrealDbRecord
    {
        [CborProperty("value")]
        public ulong Value { get; set; }

        [CborProperty("nullable")]
        public ulong? Nullable { get; set; }
    }
}
