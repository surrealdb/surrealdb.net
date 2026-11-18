using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models.Auth;

namespace SurrealDb.Net.Internals.Cbor.Converters.Auth;

/// <summary>
/// Converter for a <see cref="ScopeAuth"/> instance.
/// A type derived from <see cref="ScopeAuth"/> is a user model: it is written by its own converter.
/// </summary>
internal sealed class ScopeAuthConverter : AuthConverterBase<ScopeAuth>
{
    private const string ScopePropertyName = "sc";
    private const string AccessPropertyName = "ac";

    private readonly CborConverterRegistry _converters;

    public ScopeAuthConverter(CborOptions options)
    {
        _converters = options.Registry.ConverterRegistry;
    }

    protected override bool TryReadProperty(
        ref CborReader reader,
        ReadOnlySpan<byte> key,
        ScopeAuth value
    )
    {
        if (key.SequenceEqual("ns"u8))
        {
            value.Namespace = reader.ReadString()!;
            return true;
        }

        if (key.SequenceEqual("db"u8))
        {
            value.Database = reader.ReadString()!;
            return true;
        }

        if (key.SequenceEqual("sc"u8))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            value.Scope = reader.ReadString()!;
#pragma warning restore CS0618 // Type or member is obsolete
            return true;
        }

        if (key.SequenceEqual("ac"u8))
        {
            value.Access = reader.ReadString()!;
            return true;
        }

        return false;
    }

    protected override void WriteProperties(ref CborWriter writer, ScopeAuth value)
    {
        var type = value.GetType();
        if (type != typeof(ScopeAuth))
        {
            _converters.Lookup(type).Write(ref writer, value);
            return;
        }

        writer.WriteBeginMap(4);
        WriteProperty(ref writer, NamespacePropertyName, value.Namespace);
        WriteProperty(ref writer, DatabasePropertyName, value.Database);
#pragma warning disable CS0618 // Type or member is obsolete
        WriteProperty(ref writer, ScopePropertyName, value.Scope);
#pragma warning restore CS0618 // Type or member is obsolete
        WriteProperty(ref writer, AccessPropertyName, value.Access);
    }
}
