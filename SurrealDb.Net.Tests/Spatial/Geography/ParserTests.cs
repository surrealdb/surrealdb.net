using System.Text;
using Microsoft.Spatial;

namespace SurrealDb.Net.Tests.Spatial.Geography;

public class GeographyPointRecord : Record<GeographyPoint> { }

public class GeographyLineStringRecord : Record<GeographyLineString> { }

public class GeographyPolygonRecord : Record<GeographyPolygon> { }

public class GeographyMultiPointRecord : Record<GeographyMultiPoint> { }

public class GeographyMultiLineStringRecord : Record<GeographyMultiLineString> { }

public class GeographyMultiPolygonRecord : Record<GeographyMultiPolygon> { }

public class GeographyCollectionRecord : Record<GeographyCollection> { }

public class ParserTests
{
    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeographyPointFromTuple(string connectionString)
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

        var record = await client.Select<GeographyPointRecord>(("geometry", "PointFromTuple"));

        record.Should().NotBeNull();
        record!.Value.Should().NotBeNull();
        record.Value!.Longitude.Should().Be(-0.118092);
        record.Value!.Latitude.Should().Be(51.509865);
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeographyPoint(string connectionString)
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

        var record = await client.Select<GeographyPointRecord>(("geometry", "Point"));

        record.Should().NotBeNull();
        record!.Value.Should().NotBeNull();
        record.Value!.Longitude.Should().Be(-0.118092);
        record.Value!.Latitude.Should().Be(51.509865);
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeographyLineString(string connectionString)
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

        var record = await client.Select<GeographyLineStringRecord>(("geometry", "LineString"));

        record.Should().NotBeNull();
        record!.Value.Should().NotBeNull();
        record.Value!.Points.Should().HaveCount(2);

        var firstPoint = record.Value.Points.First();
        firstPoint.Longitude.Should().Be(10);
        firstPoint.Latitude.Should().Be(11.2);

        var lastPoint = record.Value.Points.Last();
        lastPoint.Longitude.Should().Be(10.5);
        lastPoint.Latitude.Should().Be(11.9);
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeographyPolygon(string connectionString)
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

        var record = await client.Select<GeographyPolygonRecord>(("geometry", "Polygon"));

        record.Should().NotBeNull();
        record!.Value.Should().NotBeNull();
        record.Value!.Rings.Should().HaveCount(1);

        var ring = record.Value.Rings.First();
        var points = ring.Points;

        points.Should().HaveCount(5);

        var firstPoint = points.First();
        firstPoint.Longitude.Should().Be(-0.38314819);
        firstPoint.Latitude.Should().Be(51.37692386);

        var secondPoint = points.Skip(1).First();
        secondPoint.Longitude.Should().Be(0.1785278);
        secondPoint.Latitude.Should().Be(51.37692386);

        var thirdPoint = points.Skip(2).First();
        thirdPoint.Longitude.Should().Be(0.1785278);
        thirdPoint.Latitude.Should().Be(51.61460570);

        var fourthPoint = points.Skip(3).First();
        fourthPoint.Longitude.Should().Be(-0.38314819);
        fourthPoint.Latitude.Should().Be(51.61460570);

        var lastPoint = points.Last();
        lastPoint.Longitude.Should().Be(-0.38314819);
        lastPoint.Latitude.Should().Be(51.37692386);
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeographyMultiPoint(string connectionString)
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

        var record = await client.Select<GeographyMultiPointRecord>(("geometry", "MultiPoint"));

        record.Should().NotBeNull();
        record!.Value.Should().NotBeNull();

        var points = record.Value!.Points;
        points.Should().HaveCount(2);

        var firstPoint = points.First();
        firstPoint.Longitude.Should().Be(10);
        firstPoint.Latitude.Should().Be(11.2);

        var lastPoint = points.Last();
        lastPoint.Longitude.Should().Be(10.5);
        lastPoint.Latitude.Should().Be(11.9);
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeographyMultiLineString(string connectionString)
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

        var record = await client.Select<GeographyMultiLineStringRecord>(
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
            firstPoint.Longitude.Should().Be(10);
            firstPoint.Latitude.Should().Be(11.2);

            var secondPoint = firstLineString.Points.Last();
            secondPoint.Longitude.Should().Be(10.5);
            secondPoint.Latitude.Should().Be(11.9);
        }

        var secondLineString = lineStrings.Last();
        secondLineString.Points.Should().HaveCount(3);

        {
            var firstPoint = secondLineString.Points.First();
            firstPoint.Longitude.Should().Be(11);
            firstPoint.Latitude.Should().Be(12.2);

            var secondPoint = secondLineString.Points[1];
            secondPoint.Longitude.Should().Be(11.5);
            secondPoint.Latitude.Should().Be(12.9);

            var thirdPoint = secondLineString.Points.Last();
            thirdPoint.Longitude.Should().Be(12);
            thirdPoint.Latitude.Should().Be(13);
        }
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeographyMultiPolygon(string connectionString)
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

        var record = await client.Select<GeographyMultiPolygonRecord>(("geometry", "MultiPolygon"));

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
            firstPoint.Longitude.Should().Be(10);
            firstPoint.Latitude.Should().Be(11.2);

            var secondPoint = firstRing.Points[1];
            secondPoint.Longitude.Should().Be(10.5);
            secondPoint.Latitude.Should().Be(11.9);

            var thirdPoint = firstRing.Points[2];
            thirdPoint.Longitude.Should().Be(10.8);
            thirdPoint.Latitude.Should().Be(12);

            var lastPoint = firstRing.Points.Last();
            lastPoint.Longitude.Should().Be(10);
            lastPoint.Latitude.Should().Be(11.2);
        }

        var secondPolygon = polygons.Last();
        secondPolygon.Rings.Should().HaveCount(1);

        {
            var firstRing = secondPolygon.Rings.First();
            firstRing.Points.Should().HaveCount(4);

            var firstPoint = firstRing.Points.First();
            firstPoint.Longitude.Should().Be(9);
            firstPoint.Latitude.Should().Be(11.2);

            var secondPoint = firstRing.Points[1];
            secondPoint.Longitude.Should().Be(10.5);
            secondPoint.Latitude.Should().Be(11.9);

            var thirdPoint = firstRing.Points[2];
            thirdPoint.Longitude.Should().Be(10.3);
            thirdPoint.Latitude.Should().Be(13);

            var lastPoint = firstRing.Points.Last();
            lastPoint.Longitude.Should().Be(9);
            lastPoint.Latitude.Should().Be(11.2);
        }
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=rocksdb://")]
    [InlineData("Endpoint=surrealkv://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root")]
    public async Task ShouldParseGeographyCollection(string connectionString)
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

        var record = await client.Select<GeographyCollectionRecord>(
            ("geometry", "GeometryCollection")
        );

        record.Should().NotBeNull();
        record!.Value.Should().NotBeNull();

        record.Value!.Geographies.Should().HaveCount(3);

        var firstGeography = record.Value!.Geographies.First();

        {
            var multiPoint = firstGeography as GeographyMultiPoint;
            multiPoint!.Points.Should().HaveCount(2);

            var firstPoint = multiPoint.Points.First();
            firstPoint.Longitude.Should().Be(10);
            firstPoint.Latitude.Should().Be(11.2);

            var secondPoint = multiPoint.Points.Last();
            secondPoint.Longitude.Should().Be(10.5);
            secondPoint.Latitude.Should().Be(11.9);
        }

        var secondGeography = record.Value!.Geographies[1];

        {
            var polygon = secondGeography as GeographyPolygon;
            polygon!.Rings.Should().HaveCount(1);

            var ring = polygon.Rings.First();
            ring.Points.Should().HaveCount(5);

            var firstPoint = ring.Points.First();
            firstPoint.Longitude.Should().Be(-0.38314819);
            firstPoint.Latitude.Should().Be(51.37692386);

            var secondPoint = ring.Points[1];
            secondPoint.Longitude.Should().Be(0.1785278);
            secondPoint.Latitude.Should().Be(51.37692386);

            var thirdPoint = ring.Points[2];
            thirdPoint.Longitude.Should().Be(0.1785278);
            thirdPoint.Latitude.Should().Be(51.61460570);

            var fourthPoint = ring.Points[3];
            fourthPoint.Longitude.Should().Be(-0.38314819);
            fourthPoint.Latitude.Should().Be(51.61460570);

            var lastPoint = ring.Points.Last();
            lastPoint.Longitude.Should().Be(-0.38314819);
            lastPoint.Latitude.Should().Be(51.37692386);
        }

        var thirdGeography = record.Value!.Geographies[2];

        {
            var multiPolygon = thirdGeography as GeographyMultiPolygon;
            multiPolygon!.Polygons.Should().HaveCount(2);

            var firstPolygon = multiPolygon.Polygons.First();
            firstPolygon.Rings.Should().HaveCount(1);

            {
                var ring = firstPolygon.Rings.First();
                ring.Points.Should().HaveCount(4);

                var firstPoint = ring.Points.First();
                firstPoint.Longitude.Should().Be(10);
                firstPoint.Latitude.Should().Be(11.2);

                var secondPoint = ring.Points[1];
                secondPoint.Longitude.Should().Be(10.5);
                secondPoint.Latitude.Should().Be(11.9);

                var thirdPoint = ring.Points[2];
                thirdPoint.Longitude.Should().Be(10.8);
                thirdPoint.Latitude.Should().Be(12);

                var lastPoint = ring.Points.Last();
                lastPoint.Longitude.Should().Be(10);
                lastPoint.Latitude.Should().Be(11.2);
            }

            var lastPolygon = multiPolygon.Polygons.Last();
            lastPolygon.Rings.Should().HaveCount(1);

            {
                var ring = lastPolygon.Rings.First();
                ring.Points.Should().HaveCount(4);

                var firstPoint = ring.Points.First();
                firstPoint.Longitude.Should().Be(9);
                firstPoint.Latitude.Should().Be(11.2);

                var secondPoint = ring.Points[1];
                secondPoint.Longitude.Should().Be(10.5);
                secondPoint.Latitude.Should().Be(11.9);

                var thirdPoint = ring.Points[2];
                thirdPoint.Longitude.Should().Be(10.3);
                thirdPoint.Latitude.Should().Be(13);

                var lastPoint = ring.Points.Last();
                lastPoint.Longitude.Should().Be(9);
                lastPoint.Latitude.Should().Be(11.2);
            }
        }
    }
}
