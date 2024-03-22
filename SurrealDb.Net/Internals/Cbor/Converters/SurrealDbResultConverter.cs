using System.Net;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Parsers;
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
        string? timeString = null;
        string? details = null;
        string? description = null;
        string? information = null;
        int? code = null;
        ReadOnlyMemory<byte> result = default;

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            var key = reader.ReadString();

            switch (key)
            {
                case SurrealDbResultConstants.IdPropertyName:
                    id = reader.ReadString();
                    break;
                case SurrealDbResultConstants.StatusPropertyName:
                    status = reader.ReadString();
                    break;
                case SurrealDbResultConstants.ErrorDetailsPropertyName:
                    errorDetails = reader.ReadString();
                    break;
                case SurrealDbResultConstants.TimePropertyName:
                    timeString = reader.ReadString();
                    break;
                case SurrealDbResultConstants.DetailsPropertyName:
                    details = reader.ReadString();
                    break;
                case SurrealDbResultConstants.DescriptionPropertyName:
                    description = reader.ReadString();
                    break;
                case SurrealDbResultConstants.InformationPropertyName:
                    information = reader.ReadString();
                    break;
                case SurrealDbResultConstants.ResultPropertyName:
                    result = reader.ReadDataItemAsMemory();
                    break;
                default:
                    throw new CborException(
                        $"{key} is not a valid property of {nameof(ISurrealDbResult)}."
                    );
            }
        }

        if (status is not null)
        {
            var time = timeString is not null ? TimeSpanParser.Parse(timeString) : TimeSpan.Zero;

            if (status == SurrealDbResultConstants.OkStatus && !result.IsEmpty)
            {
                return new SurrealDbOkResult(time, status, result, _options);
            }

            if (status is not null)
            {
                return new SurrealDbErrorResult(time, status, errorDetails!);
            }
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
