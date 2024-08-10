using Dahomey.Cbor;

namespace SurrealDb.Net.Models;

/// <summary>
/// A non-generic base implementation for the <see cref="RecordId"/> type.
/// Reflects a record ID (that contains both the record's table name and id).
/// </summary>
public partial class RecordId : IEquatable<RecordId>
{
    private readonly CborOptions? _options;

    internal readonly ReadOnlyMemory<byte>? _serializedCborId;

    /// <summary>
    /// Table part of the record id.
    /// </summary>
    public string Table { get; private set; }

    internal RecordId(string table)
    {
        Table = table;
    }

    internal RecordId(string table, CborOptions options)
        : this(table)
    {
        _options = options;
    }

    internal RecordId(string table, ReadOnlyMemory<byte> id, CborOptions options)
        : this(table, options)
    {
        _serializedCborId = id;
    }

    /// <summary>
    /// Deserialize the non-generic id part stored in the record id to the type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Expected type of the id part.</typeparam>
    /// <exception cref="InvalidOperationException"></exception>
    public virtual T DeserializeId<T>()
    {
        if (!_serializedCborId.HasValue)
        {
            throw new InvalidOperationException(
                $"Failed to execute {nameof(DeserializeId)}, the record id does not contain the serialized id part."
            );
        }

        return CborSerializer.Deserialize<T>(_serializedCborId.Value.Span, _options);
    }

    internal virtual object? DeserializeId(Type type)
    {
        if (!_serializedCborId.HasValue)
        {
            throw new InvalidOperationException(
                $"Failed to execute {nameof(DeserializeId)}, the record id does not contain the serialized id part."
            );
        }

        return CborSerializer.Deserialize(type, _serializedCborId.Value.Span, _options);
    }

    /// <summary>
    /// Try to deserialize the non-generic id part stored in the record id to the type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Expected type of the id part.</typeparam>
    /// <param name="value">The deserialized id part.</param>
    /// <returns>Returns true if deserialization succeeded.</returns>
    public virtual bool TryDeserializeId<T>(out T value)
    {
        if (!_serializedCborId.HasValue)
        {
            value = default!;
            return false;
        }

        try
        {
            // TODO : TryDeserialize method
            value = CborSerializer.Deserialize<T>(_serializedCborId.Value.Span, _options);
            return true;
        }
        catch
        {
            value = default!;
            return false;
        }
    }

    public virtual bool Equals(RecordId? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (Table != other.Table)
            return false;

        if (!_serializedCborId.HasValue && other._serializedCborId.HasValue)
            return other.Equals(this);

        if (!_serializedCborId.HasValue || !other._serializedCborId.HasValue)
            return false;

        return _serializedCborId.Value.Span.SequenceEqual(other._serializedCborId.Value.Span);
    }

    public override bool Equals(object? obj)
    {
        if (obj is RecordId other)
            return Equals(other);

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        if (!_serializedCborId.HasValue)
        {
            throw new InvalidOperationException(
                $"Failed to execute {nameof(GetHashCode)}, the record id does not contain the serialized id part."
            );
        }

        return HashCode.Combine(Table, _serializedCborId.Value);
    }
}
