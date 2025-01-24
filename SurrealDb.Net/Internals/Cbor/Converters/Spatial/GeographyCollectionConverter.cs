using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeographyCollectionConverter : CborConverterBase<GeographyCollection>
{
    private readonly ICborConverter<GeographyPoint> _pointConverter;
    private readonly ICborConverter<GeographyLineString> _lineStringConverter;
    private readonly ICborConverter<GeographyPolygon> _polygonConverter;
    private readonly ICborConverter<GeographyMultiPoint> _multiPointConverter;
    private readonly ICborConverter<GeographyMultiLineString> _multiLineStringConverter;
    private readonly ICborConverter<GeographyMultiPolygon> _multiPolygonConverter;

    public GeographyCollectionConverter(CborOptions options)
    {
        _pointConverter = options.Registry.ConverterRegistry.Lookup<GeographyPoint>();
        _lineStringConverter = options.Registry.ConverterRegistry.Lookup<GeographyLineString>();
        _polygonConverter = options.Registry.ConverterRegistry.Lookup<GeographyPolygon>();
        _multiPointConverter = options.Registry.ConverterRegistry.Lookup<GeographyMultiPoint>();
        _multiLineStringConverter =
            options.Registry.ConverterRegistry.Lookup<GeographyMultiLineString>();
        _multiPolygonConverter = options.Registry.ConverterRegistry.Lookup<GeographyMultiPolygon>();
    }

    public override GeographyCollection Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        var geographyBuilder = GeographyFactory.Collection();

        for (int i = 0; i < size; i++)
        {
            if (!reader.TryReadSemanticTag(out var semanticTag))
            {
                throw new CborException("Expected a semantic tag");
            }

            switch (semanticTag)
            {
                case CborTagConstants.TAG_GEOMETRY_POINT:
                    {
                        var point = _pointConverter.Read(ref reader);
                        geographyBuilder.Point(point.Latitude, point.Longitude);
                    }
                    break;
                case CborTagConstants.TAG_GEOMETRY_LINE:
                    {
                        var lineString = _lineStringConverter.Read(ref reader);
                        geographyBuilder.LineString();
                        foreach (var point in lineString.Points)
                        {
                            geographyBuilder.LineTo(point.Latitude, point.Longitude);
                        }
                    }
                    break;
                case CborTagConstants.TAG_GEOMETRY_POLYGON:
                    {
                        var polygon = _polygonConverter.Read(ref reader);
                        geographyBuilder.Polygon();
                        foreach (var ring in polygon.Rings)
                        {
                            var firstPoint = ring.Points.First();
                            geographyBuilder.Ring(firstPoint.Latitude, firstPoint.Longitude);
                            foreach (var point in ring.Points.Skip(1))
                            {
                                geographyBuilder.LineTo(point.Latitude, point.Longitude);
                            }
                        }
                    }
                    break;
                case CborTagConstants.TAG_GEOMETRY_MULTIPOINT:
                    {
                        var multiPoint = _multiPointConverter.Read(ref reader);
                        geographyBuilder.MultiPoint();
                        foreach (var point in multiPoint.Points)
                        {
                            geographyBuilder.Point(point.Latitude, point.Longitude);
                        }
                    }
                    break;
                case CborTagConstants.TAG_GEOMETRY_MULTILINE:
                    {
                        var multiLineString = _multiLineStringConverter.Read(ref reader);
                        geographyBuilder.MultiLineString();
                        foreach (var line in multiLineString.LineStrings)
                        {
                            geographyBuilder.LineString();
                            foreach (var point in line.Points)
                            {
                                geographyBuilder.LineTo(point.Latitude, point.Longitude);
                            }
                        }
                    }
                    break;
                case CborTagConstants.TAG_GEOMETRY_MULTIPOLYGON:
                    {
                        var multiPolygon = _multiPolygonConverter.Read(ref reader);
                        geographyBuilder.MultiPolygon();
                        foreach (var polygon in multiPolygon.Polygons)
                        {
                            geographyBuilder.Polygon();
                            foreach (var ring in polygon.Rings)
                            {
                                var firstPoint = ring.Points.First();
                                geographyBuilder.Ring(firstPoint.Latitude, firstPoint.Longitude);
                                foreach (var point in ring.Points.Skip(1))
                                {
                                    geographyBuilder.LineTo(point.Latitude, point.Longitude);
                                }
                            }
                        }
                    }
                    break;
                case CborTagConstants.TAG_GEOMETRY_COLLECTION:
                    var collection = Read(ref reader);
                    geographyBuilder.Collection();
                    foreach (var geography in collection.Geographies)
                    {
                        switch (geography)
                        {
                            case GeographyPoint point:
                                geographyBuilder.Point(point.Latitude, point.Longitude);
                                break;
                            case GeographyLineString lineString:
                                geographyBuilder.LineString();
                                foreach (var point in lineString.Points)
                                {
                                    geographyBuilder.LineTo(point.Latitude, point.Longitude);
                                }
                                break;
                            case GeographyPolygon polygon:
                                geographyBuilder.Polygon();
                                foreach (var ring in polygon.Rings)
                                {
                                    var firstPoint = ring.Points.First();
                                    geographyBuilder.Ring(
                                        firstPoint.Latitude,
                                        firstPoint.Longitude
                                    );
                                    foreach (var point in ring.Points.Skip(1))
                                    {
                                        geographyBuilder.LineTo(point.Latitude, point.Longitude);
                                    }
                                }
                                break;
                            case GeographyMultiPoint multiPoint:
                                geographyBuilder.MultiPoint();
                                foreach (var point in multiPoint.Points)
                                {
                                    geographyBuilder.Point(point.Latitude, point.Longitude);
                                }
                                break;
                            case GeographyMultiLineString multiLineString:
                                geographyBuilder.MultiLineString();
                                foreach (var line in multiLineString.LineStrings)
                                {
                                    geographyBuilder.LineString();
                                    foreach (var point in line.Points)
                                    {
                                        geographyBuilder.LineTo(point.Latitude, point.Longitude);
                                    }
                                }
                                break;
                            case GeographyMultiPolygon multiPolygon:
                                geographyBuilder.MultiPolygon();
                                foreach (var polygon in multiPolygon.Polygons)
                                {
                                    geographyBuilder.Polygon();
                                    foreach (var ring in polygon.Rings)
                                    {
                                        var firstPoint = ring.Points.First();
                                        geographyBuilder.Ring(
                                            firstPoint.Latitude,
                                            firstPoint.Longitude
                                        );
                                        foreach (var point in ring.Points.Skip(1))
                                        {
                                            geographyBuilder.LineTo(
                                                point.Latitude,
                                                point.Longitude
                                            );
                                        }
                                    }
                                }
                                break;
                            default:
                                throw new CborException(
                                    $"Cannot deserialize {nameof(GeographyCollection)}. Unknown geography type."
                                );
                        }
                    }
                    break;
                default:
                    throw new CborException("Expected a valid semantic tag");
            }
        }

        return geographyBuilder.Build();
    }

    public override void Write(ref CborWriter writer, GeographyCollection value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_GEOMETRY_COLLECTION);

        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteBeginArray(value.Geographies.Count);

        foreach (var geography in value.Geographies)
        {
            switch (geography)
            {
                case GeographyPoint point:
                    _pointConverter.Write(ref writer, point);
                    break;
                case GeographyLineString lineString:
                    _lineStringConverter.Write(ref writer, lineString);
                    break;
                case GeographyPolygon polygon:
                    _polygonConverter.Write(ref writer, polygon);
                    break;
                case GeographyMultiPoint multiPoint:
                    _multiPointConverter.Write(ref writer, multiPoint);
                    break;
                case GeographyMultiLineString multiLineString:
                    _multiLineStringConverter.Write(ref writer, multiLineString);
                    break;
                case GeographyMultiPolygon multiPolygon:
                    _multiPolygonConverter.Write(ref writer, multiPolygon);
                    break;
                case GeographyCollection collection:
                    Write(ref writer, collection);
                    break;
                default:
                    throw new CborException(
                        $"Cannot serialize {nameof(GeographyCollection)}. Unknown geography type \"{geography.GetType().Name}\""
                    );
            }
        }

        writer.WriteEndArray(value.Geographies.Count);
    }
}
