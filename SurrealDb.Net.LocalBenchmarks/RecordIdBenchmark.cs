using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.LocalBenchmarks;

/// <summary>
/// Measures the cost of the <see cref="RecordId"/> class on the hot paths it is part of:
/// creating ids, comparing and hashing them (e.g. as dictionary keys), and CBOR (de)serialization.
/// </summary>
/// <remarks>
/// Each <c>Struct_*</c> benchmark runs the same operation on <see cref="RecordIdValue{TId}"/>,
/// a benchmark-only <c>readonly struct</c> prototype. It is not part of the library:
/// it is here to show how much a struct representation (GitHub issue #173) would still gain.
/// </remarks>
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class RecordIdBenchmark
{
    private const string Table = "person";
    private const string StringId = "tobie";
    private const int IntId = 42;
    private const int RowCount = 100;

    private CborOptions _options = null!;
    private readonly ArrayBufferWriter<byte> _writer = new(256);

    private RecordId _stringId = null!;
    private RecordId _stringIdCopy = null!;
    private RecordId _intId = null!;
    private HashSet<RecordId> _set = null!;
    private RecordIdValue<string> _structStringId;
    private RecordIdValue<string> _structStringIdCopy;
    private HashSet<RecordIdValue<string>> _structSet = null!;

    private byte[] _stringIdCbor = null!;
    private byte[] _intIdCbor = null!;
    private byte[] _rowsCbor = null!;

    [GlobalSetup]
    public void Setup()
    {
        _options = SurrealDbCborOptions.GetCborSerializerOptions(options =>
        {
            options.Registry.ConverterRegistry.RegisterConverter(
                typeof(RecordIdValue<string>),
                new RecordIdValueConverter<string>(options)
            );
            options.Registry.ConverterRegistry.RegisterConverter(
                typeof(RecordIdValue<int>),
                new RecordIdValueConverter<int>(options)
            );
        });

        _stringId = RecordId.From(Table, StringId);
        _stringIdCopy = RecordId.From(Table, StringId);
        _intId = RecordId.From(Table, IntId);
        _ = _stringId.GetHashCode();
        _ = _stringIdCopy.GetHashCode();

        _set = new HashSet<RecordId>();
        _structSet = new HashSet<RecordIdValue<string>>();
        for (int i = 0; i < RowCount; i++)
        {
            _set.Add(RecordId.From(Table, $"{StringId}{i}"));
            _structSet.Add(new RecordIdValue<string>(Table, $"{StringId}{i}"));
        }
        _set.Add(_stringId);
        _structStringId = new RecordIdValue<string>(Table, StringId);
        _structStringIdCopy = new RecordIdValue<string>(Table, StringId);
        _structSet.Add(_structStringId);

        _stringIdCbor = ToCbor<RecordId>(_stringId);
        _intIdCbor = ToCbor<RecordId>(_intId);

        var rows = Enumerable
            .Range(0, RowCount)
            .Select(i => new ClassRow { Id = RecordId.From(Table, $"{StringId}{i}"), Name = "x" })
            .ToList();
        _rowsCbor = ToCbor(rows);
    }

    private byte[] ToCbor<T>(T value)
    {
        var writer = new ArrayBufferWriter<byte>();
        Cbor.Serialize(value, writer, _options);
        return writer.WrittenSpan.ToArray();
    }

    // Create

    [Benchmark(Baseline = true), BenchmarkCategory("Create")]
    public RecordId Create_StringId() => RecordId.From(Table, StringId);

    [Benchmark, BenchmarkCategory("Create")]
    public RecordId Create_IntId() => RecordId.From(Table, IntId);

    [Benchmark, BenchmarkCategory("Create")]
    public RecordIdValue<string> Struct_Create_StringId() => new(Table, StringId);

    // Equality: "Fresh" ids have never been compared or hashed, so the first comparison pays for
    // serializing the id part. "Warm" ids already did that once.

    [Benchmark(Baseline = true), BenchmarkCategory("Equals")]
    public bool Equals_Fresh_StringId() =>
        RecordId.From(Table, StringId) == RecordId.From(Table, StringId);

    [Benchmark, BenchmarkCategory("Equals")]
    public bool Equals_Fresh_IntId() => RecordId.From(Table, IntId) == RecordId.From(Table, IntId);

    [Benchmark, BenchmarkCategory("Equals")]
    public bool Equals_Warm_StringId() => _stringId == _stringIdCopy;

    [Benchmark, BenchmarkCategory("Equals")]
    public bool Struct_Equals_StringId() => _structStringId == _structStringIdCopy;

    // Hashing

    [Benchmark(Baseline = true), BenchmarkCategory("GetHashCode")]
    public int GetHashCode_Fresh_StringId() => RecordId.From(Table, StringId).GetHashCode();

    [Benchmark, BenchmarkCategory("GetHashCode")]
    public int GetHashCode_Fresh_IntId() => RecordId.From(Table, IntId).GetHashCode();

    [Benchmark, BenchmarkCategory("GetHashCode")]
    public int GetHashCode_Warm_StringId() => _stringId.GetHashCode();

    [Benchmark, BenchmarkCategory("GetHashCode")]
    public int Struct_GetHashCode_StringId() => _structStringId.GetHashCode();

    // Lookup of a new key in a set of 101 ids

    [Benchmark(Baseline = true), BenchmarkCategory("HashSet")]
    public bool HashSet_Contains_Fresh() => _set.Contains(RecordId.From(Table, StringId));

    [Benchmark, BenchmarkCategory("HashSet")]
    public bool Struct_HashSet_Contains() =>
        _structSet.Contains(new RecordIdValue<string>(Table, StringId));

    // CBOR write (declared type is RecordId, as for IRecord.Id)

    [Benchmark(Baseline = true), BenchmarkCategory("Serialize")]
    public int Serialize_StringId()
    {
        _writer.ResetWrittenCount();
        Cbor.Serialize(_stringId, _writer, _options);
        return _writer.WrittenCount;
    }

    [Benchmark, BenchmarkCategory("Serialize")]
    public int Serialize_IntId()
    {
        _writer.ResetWrittenCount();
        Cbor.Serialize(_intId, _writer, _options);
        return _writer.WrittenCount;
    }

    [Benchmark, BenchmarkCategory("Serialize")]
    public int Struct_Serialize_StringId()
    {
        _writer.ResetWrittenCount();
        Cbor.Serialize(_structStringId, _writer, _options);
        return _writer.WrittenCount;
    }

    // CBOR read

    [Benchmark(Baseline = true), BenchmarkCategory("Deserialize")]
    public RecordId Deserialize_StringId() => Cbor.Deserialize<RecordId>(_stringIdCbor, _options);

    [Benchmark, BenchmarkCategory("Deserialize")]
    public RecordId Deserialize_IntId() => Cbor.Deserialize<RecordId>(_intIdCbor, _options);

    [Benchmark, BenchmarkCategory("Deserialize")]
    public RecordIdValue<string> Struct_Deserialize_StringId() =>
        Cbor.Deserialize<RecordIdValue<string>>(_stringIdCbor, _options);

    // A select of 100 rows: { id, name }

    [Benchmark(Baseline = true), BenchmarkCategory("Deserialize100Rows")]
    public List<ClassRow> Deserialize_100Rows() =>
        Cbor.Deserialize<List<ClassRow>>(_rowsCbor, _options);

    [Benchmark, BenchmarkCategory("Deserialize100Rows")]
    public List<StructRow> Struct_Deserialize_100Rows() =>
        Cbor.Deserialize<List<StructRow>>(_rowsCbor, _options);

    // What every request pays to get the default CBOR options (HTTP and embedded engines,
    // WS responses), then serialize one record id with them.

    [Benchmark(Baseline = true), BenchmarkCategory("DefaultOptions")]
    public int DefaultOptions_SerializeRecordId()
    {
        _writer.ResetWrittenCount();
        Cbor.Serialize(_stringId, _writer, SurrealDbCborOptions.GetCborSerializerOptions(null));
        return _writer.WrittenCount;
    }
}

public class ClassRow : Record
{
    public string? Name { get; set; }
}

public class StructRow
{
    [Dahomey.Cbor.Attributes.CborProperty("id")]
    public RecordIdValue<string> Id { get; set; }
    public string? Name { get; set; }
}

/// <summary>
/// Benchmark-only prototype of a struct record id. Not part of the library.
/// </summary>
public readonly struct RecordIdValue<TId> : IEquatable<RecordIdValue<TId>>
{
    public RecordIdValue(string table, TId id)
    {
        Table = table;
        Id = id;
    }

    public string Table { get; }
    public TId Id { get; }

    public bool Equals(RecordIdValue<TId> other) =>
        string.Equals(Table, other.Table, StringComparison.Ordinal)
        && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override bool Equals(object? obj) => obj is RecordIdValue<TId> other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(StringComparer.Ordinal.GetHashCode(Table), Id);

    public static bool operator ==(RecordIdValue<TId> left, RecordIdValue<TId> right) =>
        left.Equals(right);

    public static bool operator !=(RecordIdValue<TId> left, RecordIdValue<TId> right) =>
        !left.Equals(right);
}

internal sealed class RecordIdValueConverter<TId> : CborConverterBase<RecordIdValue<TId>>
{
    private readonly CborOptions _options;
    private ICborConverter<TId>? _idConverter;

    public RecordIdValueConverter(CborOptions options)
    {
        _options = options;
    }

    private ICborConverter<TId> IdConverter =>
        _idConverter ??= _options.Registry.ConverterRegistry.Lookup<TId>();

    public override RecordIdValue<TId> Read(ref CborReader reader)
    {
        reader.ReadBeginArray();
        if (reader.ReadSize() != 2)
            throw new CborException("Expected a CBOR array with 2 elements");

        string table = reader.ReadString()!;
        var id = IdConverter.Read(ref reader);
        return new RecordIdValue<TId>(table, id);
    }

    public override void Write(ref CborWriter writer, RecordIdValue<TId> value)
    {
        writer.WriteSemanticTag(8);
        writer.WriteBeginArray(2);
        writer.WriteString(value.Table);
        IdConverter.Write(ref writer, value.Id);
    }
}
