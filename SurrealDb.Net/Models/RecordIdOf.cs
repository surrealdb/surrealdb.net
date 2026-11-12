namespace SurrealDb.Net.Models;

/// <summary>
/// Reflects a record ID (that contains both the record's table name and id).
/// Inherited implementation of <see cref="RecordId"/>
/// that enforces <see cref="RecordIdOf{TId}.Id"/> to be a generic type (<typeparamref name="TId"/>).
/// </summary>
/// <typeparam name="TId">The type of <see cref="RecordIdOf{TId}.Id"/> property.</typeparam>
public class RecordIdOf<TId> : RecordId
{
    private readonly Lazy<ReadOnlyMemory<byte>> _serializedId;

    /// <summary>
    /// Id part of the record id.
    /// </summary>
    public TId Id { get; private set; }

    /// <summary>
    /// Creates a <see cref="RecordId"/> with defined table name and id of type <typeparamref name="TId"/>.
    /// </summary>
    /// <param name="table">Table part of the record id.</param>
    /// <param name="id">Id part of the record id.</param>
    public RecordIdOf(string table, TId id)
        : base(table)
    {
        Id = id;
        _serializedId = new Lazy<ReadOnlyMemory<byte>>(
            () => SerializeId(Internals.Cbor.SurrealDbCborOptions.Default.Value)
        );
    }

    internal RecordIdOf(string table, TId id, Dahomey.Cbor.CborOptions options)
        : base(table)
    {
        Id = id;
        _serializedId = new Lazy<ReadOnlyMemory<byte>>(() => SerializeId(options));
    }

    internal override ReadOnlyMemory<byte> GetSerializedId() => _serializedId.Value;

    private ReadOnlyMemory<byte> SerializeId(Dahomey.Cbor.CborOptions options)
    {
        var bufferWriter = new System.Buffers.ArrayBufferWriter<byte>();
        Dahomey.Cbor.Cbor.Serialize(Id, bufferWriter, options);
        return bufferWriter.WrittenMemory.ToArray();
    }

    public override T DeserializeId<T>()
    {
        if (Id is T value)
            return value;

        throw new InvalidCastException($"Cannot deserialize record id part to {typeof(T).Name}");
    }

    internal override object? DeserializeId(Type type)
    {
        if (type.IsInstanceOfType(Id))
            return Id;

        throw new InvalidCastException($"Cannot deserialize record id part to {type.Name}");
    }

    public override bool TryDeserializeId<T>(out T value)
    {
        if (Id is T v)
        {
            value = v;
            return true;
        }

        value = default!;
        return false;
    }

    public override bool Equals(RecordId? other) => base.Equals(other);

    public override bool Equals(object? obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();
}
