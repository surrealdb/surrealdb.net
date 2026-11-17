using System.Buffers;
using Dahomey.Cbor;
using SurrealDb.Net.Internals.Cbor;

namespace SurrealDb.Net.Models;

/// <summary>
/// A non-generic base implementation for the <see cref="RecordId"/> type.
/// Reflects a record ID (that contains both the record's table name and id).
/// </summary>
public partial class RecordId : IEquatable<RecordId>
{
    [ThreadStatic]
    private static ArrayBufferWriter<byte>? _idBuffer;

    private const int MaxRetainedIdBufferSize = 1024;

    private readonly CborOptions? _options;

    /// <summary>
    /// The CBOR encoding of the id part.
    /// Set by the constructor for an id without a typed representation,
    /// or computed on first use (equality, hashing) for a <see cref="RecordIdOf{TId}"/>.
    /// </summary>
    private byte[]? _serializedId;

    /// <summary>
    /// Table part of the record id.
    /// </summary>
    public string Table { get; private set; }

    internal RecordId(string table)
    {
        Table = table;
    }

    internal RecordId(string table, CborOptions? options)
        : this(table)
    {
        _options = options;
    }

    internal RecordId(string table, byte[] id, CborOptions options)
        : this(table, options)
    {
        _serializedId = id;
    }

    internal ReadOnlySpan<byte> GetSerializedId()
    {
        var serializedId = _serializedId;
        if (serializedId is not null)
            return serializedId;

        serializedId = SerializeId(_options ?? SurrealDbCborOptions.Default.Value);
        return Interlocked.CompareExchange(ref _serializedId, serializedId, null) ?? serializedId;
    }

    /// <summary>
    /// Encodes the id part to CBOR. Only typed ids can do it,
    /// other ids receive their encoding from the constructor.
    /// </summary>
    private protected virtual byte[] SerializeId(CborOptions options)
    {
        throw new InvalidOperationException(
            $"Failed to execute {nameof(GetSerializedId)}, the record id does not contain the serialized id part."
        );
    }

    private protected static byte[] EncodeId<T>(T id, CborOptions options)
    {
        var buffer = _idBuffer ?? new ArrayBufferWriter<byte>(64);
        _idBuffer = null;

        try
        {
            CborSerializer.Serialize(id, buffer, options);
            return buffer.WrittenSpan.ToArray();
        }
        finally
        {
            if (buffer.Capacity <= MaxRetainedIdBufferSize)
            {
                buffer.Clear();
                _idBuffer = buffer;
            }
        }
    }

    /// <summary>
    /// Writes the CBOR encoding of the id part, as part of a record id value.
    /// </summary>
    internal virtual void WriteId(IBufferWriter<byte> buffer, CborOptions options)
    {
        if (_serializedId is null)
        {
            throw new CborException($"Cannot serialize a {nameof(RecordId)}.");
        }

        buffer.Write(_serializedId);
    }

    /// <summary>
    /// Deserialize the non-generic id part stored in the record id to the type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Expected type of the id part.</typeparam>
    /// <exception cref="InvalidOperationException"></exception>
    public virtual T DeserializeId<T>()
    {
        if (_serializedId is null)
        {
            throw new InvalidOperationException(
                $"Failed to execute {nameof(DeserializeId)}, the record id does not contain the serialized id part."
            );
        }

        return CborSerializer.Deserialize<T>(_serializedId, _options);
    }

    internal virtual object? DeserializeId(Type type)
    {
        if (_serializedId is null)
        {
            throw new InvalidOperationException(
                $"Failed to execute {nameof(DeserializeId)}, the record id does not contain the serialized id part."
            );
        }

        return CborSerializer.Deserialize(type, _serializedId, _options);
    }

    /// <summary>
    /// Try to deserialize the non-generic id part stored in the record id to the type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Expected type of the id part.</typeparam>
    /// <param name="value">The deserialized id part.</param>
    /// <returns>Returns true if deserialization succeeded.</returns>
    public virtual bool TryDeserializeId<T>(out T value)
    {
        if (_serializedId is null)
        {
            value = default!;
            return false;
        }

        try
        {
            // TODO : TryDeserialize method
            value = CborSerializer.Deserialize<T>(_serializedId, _options);
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

        if (!string.Equals(Table, other.Table, StringComparison.Ordinal))
            return false;

        return GetSerializedId().SequenceEqual(other.GetSerializedId());
    }

    public override bool Equals(object? obj)
    {
        if (obj is RecordId other)
            return Equals(other);

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Table, StringComparer.Ordinal);

#if NET8_0_OR_GREATER
        hashCode.AddBytes(GetSerializedId());
#else
        foreach (byte value in GetSerializedId())
            hashCode.Add(value);
#endif

        return hashCode.ToHashCode();
    }
}
