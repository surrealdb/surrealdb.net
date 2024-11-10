using System.Text;
using Microsoft.Spatial;

namespace SurrealDb.Net.Tests.Spatial.Geometry;

public class GeometryPointRecord : Record<GeometryPoint> { }

public class GeometryLineStringRecord : Record<GeometryLineString> { }

public class GeometryPolygonRecord : Record<GeometryPolygon> { }

public class GeometryMultiPointRecord : Record<GeometryMultiPoint> { }

public class GeometryMultiLineStringRecord : Record<GeometryMultiLineString> { }

public class GeometryMultiPolygonRecord : Record<GeometryMultiPolygon> { }

public class GeometryCollectionRecord : Record<GeometryCollection> { }

public class ParserTests
{
    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeometryPointFromTuple(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/geometry.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        var record = await client.Select<GeometryPointRecord>(("geometry", "PointFromTuple"));

        record.Should().NotBeNull();
        record!.Value.Should().NotBeNull();
        record.Value!.X.Should().Be(-0.118092);
        record.Value!.Y.Should().Be(51.509865);
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeometryPoint(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/geometry.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        var record = await client.Select<GeometryPointRecord>(("geometry", "Point"));

        record.Should().NotBeNull();
        record!.Value.Should().NotBeNull();
        record.Value!.X.Should().Be(-0.118092);
        record.Value!.Y.Should().Be(51.509865);
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeometryLineString(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/geometry.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        var record = await client.Select<GeometryLineStringRecord>(("geometry", "LineString"));

        record.Should().NotBeNull();
        record!.Value.Should().NotBeNull();
        record.Value!.Points.Should().HaveCount(2);

        var firstPoint = record.Value.Points.First();
        firstPoint.X.Should().Be(10);
        firstPoint.Y.Should().Be(11.2);

        var lastPoint = record.Value.Points.Last();
        lastPoint.X.Should().Be(10.5);
        lastPoint.Y.Should().Be(11.9);
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeometryPolygon(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/geometry.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        var record = await client.Select<GeometryPolygonRecord>(("geometry", "Polygon"));

        record.Should().NotBeNull();
        record!.Value.Should().NotBeNull();
        record.Value!.Rings.Should().HaveCount(1);

        var ring = record.Value.Rings.First();
        var points = ring.Points;

        points.Should().HaveCount(5);

        var firstPoint = points.First();
        firstPoint.X.Should().Be(-0.38314819);
        firstPoint.Y.Should().Be(51.37692386);

        var secondPoint = points.Skip(1).First();
        secondPoint.X.Should().Be(0.1785278);
        secondPoint.Y.Should().Be(51.37692386);

        var thirdPoint = points.Skip(2).First();
        thirdPoint.X.Should().Be(0.1785278);
        thirdPoint.Y.Should().Be(51.61460570);

        var fourthPoint = points.Skip(3).First();
        fourthPoint.X.Should().Be(-0.38314819);
        fourthPoint.Y.Should().Be(51.61460570);

        var lastPoint = points.Last();
        lastPoint.X.Should().Be(-0.38314819);
        lastPoint.Y.Should().Be(51.37692386);
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeometryMultiPoint(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/geometry.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        var record = await client.Select<GeometryMultiPointRecord>(("geometry", "MultiPoint"));

        record.Should().NotBeNull();
        record!.Value.Should().NotBeNull();

        var points = record.Value!.Points;
        points.Should().HaveCount(2);

        var firstPoint = points.First();
        firstPoint.X.Should().Be(10);
        firstPoint.Y.Should().Be(11.2);

        var lastPoint = points.Last();
        lastPoint.X.Should().Be(10.5);
        lastPoint.Y.Should().Be(11.9);
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeometryMultiLineString(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/geometry.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        var record = await client.Select<GeometryMultiLineStringRecord>(
            ("geometry", "MultiLineString")
        );

        record.Should().NotBeNull();
        record!.Value.Should().NotBeNull();

        var lineStrings = record.Value!.LineStrings;
        lineStrings.Should().HaveCount(2);

        var firstLineString = lineStrings.First();
        firstLineString.Points.Should().HaveCount(2);

        {
            var firstPoint = firstLineString.Points.First();
            firstPoint.X.Should().Be(10);
            firstPoint.Y.Should().Be(11.2);

            var secondPoint = firstLineString.Points.Last();
            secondPoint.X.Should().Be(10.5);
            secondPoint.Y.Should().Be(11.9);
        }

        var secondLineString = lineStrings.Last();
        secondLineString.Points.Should().HaveCount(3);

        {
            var firstPoint = secondLineString.Points.First();
            firstPoint.X.Should().Be(11);
            firstPoint.Y.Should().Be(12.2);

            var secondPoint = secondLineString.Points[1];
            secondPoint.X.Should().Be(11.5);
            secondPoint.Y.Should().Be(12.9);

            var thirdPoint = secondLineString.Points.Last();
            thirdPoint.X.Should().Be(12);
            thirdPoint.Y.Should().Be(13);
        }
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeometryMultiPolygon(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/geometry.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        var record = await client.Select<GeometryMultiPolygonRecord>(("geometry", "MultiPolygon"));

        record.Should().NotBeNull();
        record!.Value.Should().NotBeNull();

        var polygons = record.Value!.Polygons;
        polygons.Should().HaveCount(2);

        var firstPolygon = polygons.First();
        firstPolygon.Rings.Should().HaveCount(1);

        {
            var firstRing = firstPolygon.Rings.First();
            firstRing.Points.Should().HaveCount(4);

            var firstPoint = firstRing.Points.First();
            firstPoint.X.Should().Be(10);
            firstPoint.Y.Should().Be(11.2);

            var secondPoint = firstRing.Points[1];
            secondPoint.X.Should().Be(10.5);
            secondPoint.Y.Should().Be(11.9);

            var thirdPoint = firstRing.Points[2];
            thirdPoint.X.Should().Be(10.8);
            thirdPoint.Y.Should().Be(12);

            var lastPoint = firstRing.Points.Last();
            lastPoint.X.Should().Be(10);
            lastPoint.Y.Should().Be(11.2);
        }

        var secondPolygon = polygons.Last();
        secondPolygon.Rings.Should().HaveCount(1);

        {
            var firstRing = secondPolygon.Rings.First();
            firstRing.Points.Should().HaveCount(4);

            var firstPoint = firstRing.Points.First();
            firstPoint.X.Should().Be(9);
            firstPoint.Y.Should().Be(11.2);

            var secondPoint = firstRing.Points[1];
            secondPoint.X.Should().Be(10.5);
            secondPoint.Y.Should().Be(11.9);

            var thirdPoint = firstRing.Points[2];
            thirdPoint.X.Should().Be(10.3);
            thirdPoint.Y.Should().Be(13);

            var lastPoint = firstRing.Points.Last();
            lastPoint.X.Should().Be(9);
            lastPoint.Y.Should().Be(11.2);
        }
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeometryCollection(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Schemas/geometry.surql"
        );
        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

        string query = fileContent;

        using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        (await client.RawQuery(query)).EnsureAllOks();

        var record = await client.Select<GeometryCollectionRecord>(
            ("geometry", "GeometryCollection")
        );

        record.Should().NotBeNull();
        record!.Value.Should().NotBeNull();

        record.Value!.Geometries.Should().HaveCount(3);

        var firstGeometry = record.Value!.Geometries.First();

        {
            var multiPoint = firstGeometry as GeometryMultiPoint;
            multiPoint!.Points.Should().HaveCount(2);

            var firstPoint = multiPoint.Points.First();
            firstPoint.X.Should().Be(10);
            firstPoint.Y.Should().Be(11.2);

            var secondPoint = multiPoint.Points.Last();
            secondPoint.X.Should().Be(10.5);
            secondPoint.Y.Should().Be(11.9);
        }

        var secondGeometry = record.Value!.Geometries[1];

        {
            var polygon = secondGeometry as GeometryPolygon;
            polygon!.Rings.Should().HaveCount(1);

            var ring = polygon.Rings.First();
            ring.Points.Should().HaveCount(5);

            var firstPoint = ring.Points.First();
            firstPoint.X.Should().Be(-0.38314819);
            firstPoint.Y.Should().Be(51.37692386);

            var secondPoint = ring.Points[1];
            secondPoint.X.Should().Be(0.1785278);
            secondPoint.Y.Should().Be(51.37692386);

            var thirdPoint = ring.Points[2];
            thirdPoint.X.Should().Be(0.1785278);
            thirdPoint.Y.Should().Be(51.61460570);

            var fourthPoint = ring.Points[3];
            fourthPoint.X.Should().Be(-0.38314819);
            fourthPoint.Y.Should().Be(51.61460570);

            var lastPoint = ring.Points.Last();
            lastPoint.X.Should().Be(-0.38314819);
            lastPoint.Y.Should().Be(51.37692386);
        }

        var thirdGeometry = record.Value!.Geometries[2];

        {
            var multiPolygon = thirdGeometry as GeometryMultiPolygon;
            multiPolygon!.Polygons.Should().HaveCount(2);

            var firstPolygon = multiPolygon.Polygons.First();
            firstPolygon.Rings.Should().HaveCount(1);

            {
                var ring = firstPolygon.Rings.First();
                ring.Points.Should().HaveCount(4);

                var firstPoint = ring.Points.First();
                firstPoint.X.Should().Be(10);
                firstPoint.Y.Should().Be(11.2);

                var secondPoint = ring.Points[1];
                secondPoint.X.Should().Be(10.5);
                secondPoint.Y.Should().Be(11.9);

                var thirdPoint = ring.Points[2];
                thirdPoint.X.Should().Be(10.8);
                thirdPoint.Y.Should().Be(12);

                var lastPoint = ring.Points.Last();
                lastPoint.X.Should().Be(10);
                lastPoint.Y.Should().Be(11.2);
            }

            var lastPolygon = multiPolygon.Polygons.Last();
            lastPolygon.Rings.Should().HaveCount(1);

            {
                var ring = lastPolygon.Rings.First();
                ring.Points.Should().HaveCount(4);

                var firstPoint = ring.Points.First();
                firstPoint.X.Should().Be(9);
                firstPoint.Y.Should().Be(11.2);

                var secondPoint = ring.Points[1];
                secondPoint.X.Should().Be(10.5);
                secondPoint.Y.Should().Be(11.9);

                var thirdPoint = ring.Points[2];
                thirdPoint.X.Should().Be(10.3);
                thirdPoint.Y.Should().Be(13);

                var lastPoint = ring.Points.Last();
                lastPoint.X.Should().Be(9);
                lastPoint.Y.Should().Be(11.2);
            }
        }
    }
}
