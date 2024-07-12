﻿using System.Text;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Ws;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class SurrealDbWsLiveResponseContentConverter
    : CborConverterBase<SurrealDbWsLiveResponseContent>
{
    private readonly CborOptions _options;
    private readonly ICborConverter<Guid> _guidConverter;

    public SurrealDbWsLiveResponseContentConverter(CborOptions options)
    {
        _options = options;

        _guidConverter = options.Registry.ConverterRegistry.Lookup<Guid>();
    }

    public override SurrealDbWsLiveResponseContent Read(ref CborReader reader)
    {
        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        Guid? id = null;
        string? action = null;
        ReadOnlyMemory<byte>? result = null;

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            ReadOnlySpan<byte> key = reader.ReadRawString();

            if (key.SequenceEqual("id"u8))
            {
                id = _guidConverter.Read(ref reader);
                continue;
            }

            if (key.SequenceEqual("action"u8))
            {
                action = reader.ReadString();
                continue;
            }

            if (key.SequenceEqual("result"u8))
            {
                result = reader.ReadDataItemAsMemory();
                continue;
            }

            throw new CborException(
                $"{Encoding.Unicode.GetString(key)} is not a valid property of {nameof(SurrealDbWsLiveResponseContent)}."
            );
        }

        if (result.HasValue && id.HasValue && action is not null)
        {
            return new SurrealDbWsLiveResponseContent(id.Value, action!, result.Value, _options);
        }

        throw new CborException("Expected a valid response content");
    }

    public override void Write(ref CborWriter writer, SurrealDbWsLiveResponseContent value)
    {
        throw new NotSupportedException(
            $"Cannot write {nameof(SurrealDbWsLiveResponseContent)} back in cbor..."
        );
    }
}
