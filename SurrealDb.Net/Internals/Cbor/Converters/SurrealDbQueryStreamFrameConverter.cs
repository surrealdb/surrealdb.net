using System.Text;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Internals.Errors;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Internals.Cbor.Converters;

/// <summary>
/// Reads a frame of a <c>query_stream</c> answer: a map tagged by its <c>stream</c> key.
/// </summary>
internal sealed class SurrealDbQueryStreamFrameConverter
    : CborConverterBase<SurrealDbQueryStreamFrame>
{
    private readonly CborOptions _options;

    public SurrealDbQueryStreamFrameConverter(CborOptions options)
    {
        _options = options;
    }

    public override SurrealDbQueryStreamFrame Read(ref CborReader reader)
    {
        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        string? stream = null;
        int index = 0;
        int statements = 0;
        int results = 0;
        bool single = false;
        TimeSpan time = TimeSpan.Zero;
        var type = SurrealDbResponseType.Other;
        ReadOnlyMemory<byte>? payload = null;
        ReadOnlyMemory<byte>? error = null;

        // 💡 Keys are sorted, so the "stream" tag is not necessarily the first one
        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            ReadOnlySpan<byte> key = reader.ReadRawString();

            if (key.SequenceEqual("stream"u8))
            {
                stream = reader.ReadString();
            }
            else if (key.SequenceEqual("index"u8))
            {
                index = reader.ReadInt32();
            }
            else if (key.SequenceEqual("statements"u8))
            {
                statements = reader.ReadInt32();
            }
            else if (key.SequenceEqual("results"u8))
            {
                results = reader.ReadInt32();
            }
            else if (key.SequenceEqual("single"u8))
            {
                single = reader.ReadBoolean();
            }
            else if (key.SequenceEqual("time"u8))
            {
                time = new Duration(reader.ReadRawString(), true).ToTimeSpan();
            }
            else if (key.SequenceEqual("type"u8))
            {
                if (reader.GetCurrentDataItemType() == CborDataItemType.String)
                {
                    type = SurrealDbResponseTypeExtensions.From(reader.ReadString()) ?? type;
                }
                else
                {
                    reader.SkipDataItem();
                }
            }
            else if (key.SequenceEqual("values"u8) || key.SequenceEqual("value"u8))
            {
                payload = reader.ReadDataItemAsMemory();
            }
            else if (key.SequenceEqual("error"u8))
            {
                error = reader.ReadDataItemAsMemory();
            }
            else
            {
                // 💡 e.g. "version", or a field added by a newer server
                reader.SkipDataItem();
            }
        }

        return stream switch
        {
            "begin" => new SurrealDbQueryStreamBeginFrame(statements),
            "rows" => new SurrealDbQueryStreamRowsFrame(
                index,
                payload ?? throw new CborException("Expected values for a rows frame"),
                _options
            ),
            "value" => new SurrealDbQueryStreamValueFrame(
                index,
                payload ?? throw new CborException("Expected a value for a value frame"),
                _options
            ),
            "finished" => new SurrealDbQueryStreamFinishedFrame(
                index,
                time,
                type,
                single,
                ReadError(error)
            ),
            "end" => new SurrealDbQueryStreamEndFrame(results, time, ReadError(error)),
            _ => throw new CborException($"'{stream}' is not a valid stream frame."),
        };
    }

    private SurrealDbException? ReadError(ReadOnlyMemory<byte>? error)
    {
        if (!error.HasValue)
        {
            return null;
        }

        var content = CborSerializer.Deserialize<RpcErrorResponseContent>(
            error.Value.Span,
            _options
        );
        return content.ToException();
    }

    public override void Write(ref CborWriter writer, SurrealDbQueryStreamFrame value)
    {
        throw new NotSupportedException($"Cannot write {nameof(SurrealDbQueryStreamFrame)}.");
    }
}
