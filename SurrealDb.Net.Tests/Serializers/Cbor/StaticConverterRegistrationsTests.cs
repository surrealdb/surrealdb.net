using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Cbor.Converters;
#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
#else
using SystemTextJsonPatch;
#endif

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class StaticConverterRegistrationsTests : BaseCborConverterTests
{
    private static CborOptions CreateOptions()
    {
        // A new registry for each test, so that no converter comes from a previous lookup
        return SurrealDbCborOptions.GetCborSerializerOptions(_ => { });
    }

    [Test]
    public void DeserializeEnumerable()
    {
        var options = CreateOptions();

        // [1, 2, 3]
        var result = StaticConverterRegistrations.DeserializeEnumerable<int>(
            StringToByteArray("83010203"),
            options
        );

        result.Should().Equal(1, 2, 3);
        options
            .Registry.ConverterRegistry.Lookup<IEnumerable<int>>()
            .Should()
            .BeOfType<InterfaceCollectionConverter<List<int>, IEnumerable<int>, int>>();
    }

    [Test]
    public void DeserializeEnumerableOfRecords()
    {
        var options = CreateOptions();

        // [{"id": 8(["post", 1]), "Name": "first"}]
        var result = StaticConverterRegistrations
            .DeserializeEnumerable<Post>(
                StringToByteArray("81a2626964c88264706f737401644e616d65656669727374"),
                options
            )
            .ToList();

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(RecordId.From("post", 1));
        result[0].Name.Should().Be("first");
    }

    [Test]
    public void EnsureRecordIdRange()
    {
        var options = CreateOptions();

        StaticConverterRegistrations.EnsureRecordIdRange<int, int>(options);

        var converters = options.Registry.ConverterRegistry;
        converters.Lookup<RangeBound<int>>().Should().BeOfType<RangeBoundConverter<int>>();
        converters.Lookup<Range<int, int>>().Should().BeOfType<RangeConverter<int, int>>();
        converters
            .Lookup<RecordIdRange<int, int>>()
            .Should()
            .BeOfType<RecordIdRangeConverter<int, int>>();
    }

    [Test]
    public void EnsureJsonPatchDocument()
    {
        var options = CreateOptions();

        StaticConverterRegistrations.EnsureJsonPatchDocument<Post>(options);

        options
            .Registry.ConverterRegistry.Lookup<JsonPatchDocument<Post>>()
            .Should()
            .BeOfType<JsonPatchDocumentConverter<Post>>();
    }

    [Test]
    public void ShouldKeepAConverterRegisteredBefore()
    {
        var customConverter = new CustomEnumerableConverter();
        var options = SurrealDbCborOptions.GetCborSerializerOptions(o =>
            o.Registry.ConverterRegistry.RegisterConverter(
                typeof(IEnumerable<string>),
                customConverter
            )
        );

        StaticConverterRegistrations.EnsureEnumerable<string>(options);

        options
            .Registry.ConverterRegistry.Lookup<IEnumerable<string>>()
            .Should()
            .BeSameAs(customConverter);
    }

    [Test]
    public void ShouldRegisterOncePerOptions()
    {
        var options = CreateOptions();

        StaticConverterRegistrations.EnsureEnumerable<long>(options);
        var converter = options.Registry.ConverterRegistry.Lookup<IEnumerable<long>>();

        StaticConverterRegistrations.EnsureEnumerable<long>(options);

        options.Registry.ConverterRegistry.Lookup<IEnumerable<long>>().Should().BeSameAs(converter);
    }

    private sealed class Post : SurrealDbRecord
    {
        public string? Name { get; set; }
    }

    private sealed class CustomEnumerableConverter : CborConverterBase<IEnumerable<string>>
    {
        public override IEnumerable<string> Read(ref CborReader reader)
        {
            throw new NotSupportedException();
        }

        public override void Write(ref CborWriter writer, IEnumerable<string> value)
        {
            throw new NotSupportedException();
        }
    }
}
