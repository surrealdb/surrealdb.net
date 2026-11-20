using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models.Auth;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class TokensConverter : CborConverterBase<Tokens>
{
    public override Tokens Read(ref CborReader reader)
    {
        var itemType = reader.GetCurrentDataItemType();

        switch (itemType)
        {
            case CborDataItemType.String:
                return new Tokens(reader.ReadString()!);
            case CborDataItemType.Map:
            {
                reader.ReadBeginMap();

                int remainingItemCount = reader.ReadSize();

                string? access = null;
                string? refresh = null;

                while (reader.MoveNextMapItem(ref remainingItemCount))
                {
                    ReadOnlySpan<byte> key = reader.ReadRawString();

                    if (key.SequenceEqual("access"u8))
                    {
                        access = reader.ReadString();
                        continue;
                    }

                    if (key.SequenceEqual("refresh"u8))
                    {
                        refresh = reader.ReadString();
                        continue;
                    }
                }

                return new Tokens { Access = access!, Refresh = refresh };
            }
            default:
                throw new CborException("Expected a CBOR string or a CBOR object");
        }
    }

    public override void Write(ref CborWriter writer, Tokens value)
    {
        // 💡 Same shape as the server: the access token alone, or the access and refresh tokens
        if (value.Refresh is null)
        {
            writer.WriteString(value.Access);
            return;
        }

        writer.WriteBeginMap(2);
        writer.WriteString("access");
        writer.WriteString(value.Access);
        writer.WriteString("refresh");
        writer.WriteString(value.Refresh);
        writer.WriteEndMap(2);
    }
}
