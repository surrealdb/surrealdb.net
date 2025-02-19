﻿using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeometryPointConverter : CborConverterBase<GeometryPoint>
{
    private readonly ICborConverter<double> _doubleConverter;

    public GeometryPointConverter(CborOptions options)
    {
        _doubleConverter = options.Registry.ConverterRegistry.Lookup<double>();
    }

    public override GeometryPoint Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        if (size != 2)
        {
            throw new CborException("Expected a CBOR array with 2 elements");
        }

        var x = _doubleConverter.Read(ref reader);
        var y = _doubleConverter.Read(ref reader);

        return GeometryPoint.Create(x, y);
    }

    public override void Write(ref CborWriter writer, GeometryPoint value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_GEOMETRY_POINT);

        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteBeginArray(2);

        writer.WriteDouble(value.X);
        writer.WriteDouble(value.Y);

        writer.WriteEndArray(2);
    }
}
