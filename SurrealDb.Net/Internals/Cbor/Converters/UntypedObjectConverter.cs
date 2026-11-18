using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Mappings;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

/// <summary>
/// Converter for values declared as <see cref="object"/>, such as RPC parameters and query variables.
/// </summary>
/// <remarks>
/// A value is written with the converter of its runtime type, as Dahomey's reflection-built
/// <c>ObjectConverter&lt;object&gt;</c> does, so the bytes do not change. Two shapes that the
/// library itself sends are handled here instead of through the converter registry, because the
/// registry would build their converters with <c>MakeGenericType</c>, which fails under Native AOT:
/// <list type="bullet">
/// <item>every <see cref="RecordId"/> subclass, such as <see cref="RecordIdOf{TId}"/> of
/// <see cref="int"/>, is written by the <see cref="RecordId"/> converter;</item>
/// <item>a string-keyed dictionary of objects, such as query variables, is written as a map;</item>
/// <item>a collection of record ids, such as the <c>in</c> and <c>out</c> of <c>relate</c>,
/// is written as an array.</item>
/// </list>
/// </remarks>
internal sealed class UntypedObjectConverter : CborConverterBase<object?>
{
    private readonly CborConverterRegistry _converters;
    private readonly ObjectConverter<object> _plainObjectConverter;
    private ICborConverter<RecordId>? _recordIdConverter;

    public UntypedObjectConverter(CborOptions options)
    {
        _converters = options.Registry.ConverterRegistry;

        // A plain object has no members. Registering its mapping by hand, and passing a factory,
        // keeps the converter away from the reflection-based object mapping convention.
        options.Registry.ObjectMappingRegistry.Register<object>(objectMapping =>
            objectMapping.SetMemberMappings(Array.Empty<IMemberMapping>())
        );
        _plainObjectConverter = new ObjectConverter<object>(options, static () => new object());
    }

    public override object? Read(ref CborReader reader)
    {
        return _plainObjectConverter.Read(ref reader);
    }

    public override void Write(ref CborWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNull();
                return;
            case RecordId recordId:
                _recordIdConverter ??= _converters.Lookup<RecordId>();
                _recordIdConverter.Write(ref writer, recordId);
                return;
            case IReadOnlyDictionary<string, object?> map:
                WriteMap(ref writer, map);
                return;
            case IEnumerable<RecordId> recordIds:
                WriteRecordIds(ref writer, recordIds);
                return;
        }

        var type = value.GetType();
        if (type == typeof(object))
        {
            _plainObjectConverter.Write(ref writer, value);
            return;
        }

        _converters.Lookup(type).Write(ref writer, value);
    }

    private void WriteMap(ref CborWriter writer, IReadOnlyDictionary<string, object?> map)
    {
        writer.WriteBeginMap(map.Count);

        foreach (var pair in map)
        {
            writer.WriteString(pair.Key);
            Write(ref writer, pair.Value);
        }
    }

    private void WriteRecordIds(ref CborWriter writer, IEnumerable<RecordId> recordIds)
    {
        var items = recordIds as IReadOnlyCollection<RecordId> ?? recordIds.ToArray();

        writer.WriteBeginArray(items.Count);

        foreach (var recordId in items)
        {
            Write(ref writer, recordId);
        }
    }
}
