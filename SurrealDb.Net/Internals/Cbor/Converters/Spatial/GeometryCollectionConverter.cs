using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Microsoft.Spatial;

namespace SurrealDb.Net.Internals.Cbor.Converters.Spatial;

internal class GeometryCollectionConverter : CborConverterBase<GeometryCollection>
{
    private readonly ICborConverter<GeometryPoint> _pointConverter;
    private readonly ICborConverter<GeometryLineString> _lineStringConverter;
    private readonly ICborConverter<GeometryPolygon> _polygonConverter;
    private readonly ICborConverter<GeometryMultiPoint> _multiPointConverter;
    private readonly ICborConverter<GeometryMultiLineString> _multiLineStringConverter;
    private readonly ICborConverter<GeometryMultiPolygon> _multiPolygonConverter;

    public GeometryCollectionConverter(CborOptions options)
    {
        _pointConverter = options.Registry.ConverterRegistry.Lookup<GeometryPoint>();
        _lineStringConverter = options.Registry.ConverterRegistry.Lookup<GeometryLineString>();
        _polygonConverter = options.Registry.ConverterRegistry.Lookup<GeometryPolygon>();
        _multiPointConverter = options.Registry.ConverterRegistry.Lookup<GeometryMultiPoint>();
        _multiLineStringConverter =
            options.Registry.ConverterRegistry.Lookup<GeometryMultiLineString>();
        _multiPolygonConverter = options.Registry.ConverterRegistry.Lookup<GeometryMultiPolygon>();
    }

    public override GeometryCollection Read(ref CborReader reader)
    {
        reader.ReadBeginArray();

        int size = reader.ReadSize();

        var geometryBuilder = GeometryFactory.Collection();

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
                        geometryBuilder.Point(point.X, point.Y);
                    }
                    break;
                case CborTagConstants.TAG_GEOMETRY_LINE:

                    {
                        var lineString = _lineStringConverter.Read(ref reader);
                        geometryBuilder.LineString();
                        foreach (var point in lineString.Points)
                        {
                            geometryBuilder.LineTo(point.X, point.Y);
                        }
                    }
                    break;
                case CborTagConstants.TAG_GEOMETRY_POLYGON:

                    {
                        var polygon = _polygonConverter.Read(ref reader);
                        geometryBuilder.Polygon();
                        foreach (var ring in polygon.Rings)
                        {
                            var firstPoint = ring.Points.First();
                            geometryBuilder.Ring(firstPoint.X, firstPoint.Y);
                            foreach (var point in ring.Points.Skip(1))
                            {
                                geometryBuilder.LineTo(point.X, point.Y);
                            }
                        }
                    }
                    break;
                case CborTagConstants.TAG_GEOMETRY_MULTIPOINT:

                    {
                        var multiPoint = _multiPointConverter.Read(ref reader);
                        geometryBuilder.MultiPoint();
                        foreach (var point in multiPoint.Points)
                        {
                            geometryBuilder.Point(point.X, point.Y);
                        }
                    }
                    break;
                case CborTagConstants.TAG_GEOMETRY_MULTILINE:

                    {
                        var multiLineString = _multiLineStringConverter.Read(ref reader);
                        geometryBuilder.MultiLineString();
                        foreach (var line in multiLineString.LineStrings)
                        {
                            geometryBuilder.LineString();
                            foreach (var point in line.Points)
                            {
                                geometryBuilder.LineTo(point.X, point.Y);
                            }
                        }
                    }
                    break;
                case CborTagConstants.TAG_GEOMETRY_MULTIPOLYGON:

                    {
                        var multiPolygon = _multiPolygonConverter.Read(ref reader);
                        geometryBuilder.MultiPolygon();
                        foreach (var polygon in multiPolygon.Polygons)
                        {
                            geometryBuilder.Polygon();
                            foreach (var ring in polygon.Rings)
                            {
                                var firstPoint = ring.Points.First();
                                geometryBuilder.Ring(firstPoint.X, firstPoint.Y);
                                foreach (var point in ring.Points.Skip(1))
                                {
                                    geometryBuilder.LineTo(point.X, point.Y);
                                }
                            }
                        }
                    }
                    break;
                case CborTagConstants.TAG_GEOMETRY_COLLECTION:
                    var collection = Read(ref reader);
                    geometryBuilder.Collection();
                    foreach (var geometry in collection.Geometries)
                    {
                        switch (geometry)
                        {
                            case GeometryPoint point:
                                geometryBuilder.Point(point.X, point.Y);
                                break;
                            case GeometryLineString lineString:
                                geometryBuilder.LineString();
                                foreach (var point in lineString.Points)
                                {
                                    geometryBuilder.LineTo(point.X, point.Y);
                                }
                                break;
                            case GeometryPolygon polygon:
                                geometryBuilder.Polygon();
                                foreach (var ring in polygon.Rings)
                                {
                                    var firstPoint = ring.Points.First();
                                    geometryBuilder.Ring(firstPoint.X, firstPoint.Y);
                                    foreach (var point in ring.Points.Skip(1))
                                    {
                                        geometryBuilder.LineTo(point.X, point.Y);
                                    }
                                }
                                break;
                            case GeometryMultiPoint multiPoint:
                                geometryBuilder.MultiPoint();
                                foreach (var point in multiPoint.Points)
                                {
                                    geometryBuilder.Point(point.X, point.Y);
                                }
                                break;
                            case GeometryMultiLineString multiLineString:
                                geometryBuilder.MultiLineString();
                                foreach (var line in multiLineString.LineStrings)
                                {
                                    geometryBuilder.LineString();
                                    foreach (var point in line.Points)
                                    {
                                        geometryBuilder.LineTo(point.X, point.Y);
                                    }
                                }
                                break;
                            case GeometryMultiPolygon multiPolygon:
                                geometryBuilder.MultiPolygon();
                                foreach (var polygon in multiPolygon.Polygons)
                                {
                                    geometryBuilder.Polygon();
                                    foreach (var ring in polygon.Rings)
                                    {
                                        var firstPoint = ring.Points.First();
                                        geometryBuilder.Ring(firstPoint.X, firstPoint.Y);
                                        foreach (var point in ring.Points.Skip(1))
                                        {
                                            geometryBuilder.LineTo(point.X, point.Y);
                                        }
                                    }
                                }
                                break;
                            default:
                                throw new CborException(
                                    $"Cannot deserialize {nameof(GeometryCollection)}. Unknown geometry type."
                                );
                        }
                    }
                    break;
                default:
                    throw new CborException("Expected a valid semantic tag");
            }
        }

        return geometryBuilder.Build();
    }

    public override void Write(ref CborWriter writer, GeometryCollection value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_GEOMETRY_COLLECTION);

        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteBeginArray(value.Geometries.Count);

        foreach (var geometry in value.Geometries)
        {
            switch (geometry)
            {
                case GeometryPoint point:
                    _pointConverter.Write(ref writer, point);
                    break;
                case GeometryLineString lineString:
                    _lineStringConverter.Write(ref writer, lineString);
                    break;
                case GeometryPolygon polygon:
                    _polygonConverter.Write(ref writer, polygon);
                    break;
                case GeometryMultiPoint multiPoint:
                    _multiPointConverter.Write(ref writer, multiPoint);
                    break;
                case GeometryMultiLineString multiLineString:
                    _multiLineStringConverter.Write(ref writer, multiLineString);
                    break;
                case GeometryMultiPolygon multiPolygon:
                    _multiPolygonConverter.Write(ref writer, multiPolygon);
                    break;
                case GeometryCollection collection:
                    Write(ref writer, collection);
                    break;
                default:
                    throw new CborException(
                        $"Cannot serialize {nameof(GeometryCollection)}. Unknown geometry type \"{geometry.GetType().Name}\""
                    );
            }
        }

        writer.WriteEndArray(value.Geometries.Count);
    }
}
