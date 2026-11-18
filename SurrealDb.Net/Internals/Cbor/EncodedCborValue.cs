using System.Buffers;
using Dahomey.Cbor;
using Dahomey.Cbor.Attributes;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;

namespace SurrealDb.Net.Internals.Cbor;

/// <summary>
/// A value encoded in CBOR once, and written as is each time it is sent.
/// </summary>
/// <remarks>
/// 💡 Session variables are kept so: the server has the value as it was when it was set,
/// so a later change to the object given to <c>Set</c> must not reach it when the variable is sent again
/// (with each HTTP request, after a reconnection, or to a forked session).
/// This file is also compiled into the embedded engines, which carry their own copy of the type,
/// hence the converter given by attribute instead of a converter provider.
/// </remarks>
[CborConverter(typeof(EncodedCborValueConverter))]
internal sealed class EncodedCborValue
{
    public ReadOnlyMemory<byte> Bytes { get; }

    private EncodedCborValue(ReadOnlyMemory<byte> bytes)
    {
        Bytes = bytes;
    }

    public static EncodedCborValue Create(object? value, CborOptions options)
    {
        var writer = new ArrayBufferWriter<byte>();
        Dahomey.Cbor.Cbor.Serialize(value, writer, options);

        return new EncodedCborValue(writer.WrittenSpan.ToArray());
    }
}

internal sealed class EncodedCborValueConverter : CborConverterBase<EncodedCborValue>
{
    public override EncodedCborValue Read(ref CborReader reader)
    {
        throw new NotSupportedException($"An {nameof(EncodedCborValue)} is only ever written.");
    }

    public override void Write(ref CborWriter writer, EncodedCborValue value)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.BufferWriter.Write(value.Bytes.Span);
    }
}
