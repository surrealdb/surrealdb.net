using System.Net;
using System.Text;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class SurrealDbResultConverter : CborConverterBase<ISurrealDbResult>
{
    private readonly CborOptions _options;

    public SurrealDbResultConverter(CborOptions options)
    {
        _options = options;
    }

    public override ISurrealDbResult Read(ref CborReader reader)
    {
        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();

        string? id = null;
        string? status = null;
        string? errorDetails = null;
        TimeSpan time = TimeSpan.Zero;
        string? details = null;
        string? description = null;
        string? information = null;
        short? code = null;
        ReadOnlyMemory<byte>? result = null;

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            ReadOnlySpan<byte> key = reader.ReadRawString();

            if (key.SequenceEqual("id"u8))
            {
                id = reader.ReadString();
                continue;
            }

            if (key.SequenceEqual("status"u8))
            {
                status = reader.ReadString();
                continue;
            }

            if (key.SequenceEqual("errorDetails"u8))
            {
                errorDetails = reader.ReadString();
                continue;
            }

            if (key.SequenceEqual("time"u8))
            {
                time = new Duration(reader.ReadRawString(), true).ToTimeSpan();
                continue;
            }

            if (key.SequenceEqual("detail"u8))
            {
                details = reader.ReadString();
                continue;
            }

            if (key.SequenceEqual("description"u8))
            {
                description = reader.ReadString();
                continue;
            }

            if (key.SequenceEqual("information"u8))
            {
                information = reader.ReadString();
                continue;
            }

            if (key.SequenceEqual("result"u8))
            {
                result = reader.ReadDataItemAsMemory();
                continue;
            }

            if (key.SequenceEqual("code"u8))
            {
                code = reader.ReadInt16();
                continue;
            }

            throw new CborException(
                $"{Encoding.Unicode.GetString(key)} is not a valid property of {nameof(ISurrealDbResult)}."
            );
        }

        if (status is not null)
        {
            if (status == SurrealDbResultConstants.OkStatus && result.HasValue)
            {
                return new SurrealDbOkResult(time, status, result.Value, _options);
            }

            if (result.HasValue)
            {
                string errorFromResult = CborSerializer.Deserialize<string>(result.Value.Span);
                return new SurrealDbErrorResult(time, status, errorFromResult);
            }

            return new SurrealDbErrorResult(time, status, errorDetails!);
        }

        if (
            code.HasValue
            && Enum.TryParse(code.Value.ToString(), true, out HttpStatusCode httpStatusCode)
        )
        {
            // TODO : Use Source Generator to convert a number to HttpStatusCode, instead of ".GetInt16().ToString()"
            return new SurrealDbProtocolErrorResult(
                httpStatusCode,
                details!,
                description!,
                information!
            );
        }

        return new SurrealDbUnknownResult();
    }

    public override void Write(ref CborWriter writer, ISurrealDbResult value)
    {
        throw new NotSupportedException($"Cannot write {nameof(ISurrealDbResult)} back in cbor...");
    }
}
