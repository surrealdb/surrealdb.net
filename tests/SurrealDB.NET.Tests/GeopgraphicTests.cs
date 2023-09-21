using SurrealDB.NET.Geographic;
using SurrealDB.NET.Json.Converters;
using System.Text.Json;

namespace SurrealDB.NET.Tests;

[Trait("Category", "GeoJSON")]
public sealed class GeopgraphicTests
{
	private readonly JsonSerializerOptions _options;

	public GeopgraphicTests()
	{
		_options = new JsonSerializerOptions
		{
			Converters =
			{
				new PointJsonConverter()
			},
			WriteIndented = true,
		};
	}

	[Fact(DisplayName = "GeoJSON Point serialization without altitude")]
	public void PointSerializationWithoutAltitude()
	{
		var geoPoint = new Point(30, 10);

		var geoPointJson = JsonSerializer.Serialize(geoPoint, _options);

		Assert.Equal("""
			{
			  "type": "Point",
			  "coordinates": [
			    30,
			    10
			  ]
			}
			""".ReplaceLineEndings(), geoPointJson.ReplaceLineEndings());
	}

	[Fact(DisplayName = "GeoJSON Point deserialization without altitude")]
	public void PointDeserializationWithoutAltitude()
	{
		var geoPointJson = """
			{
			  "type": "Point",
			  "coordinates": [
			    30,
			    10
			  ]
			}
			"""u8;

		var geoPoint = JsonSerializer.Deserialize<Point>(geoPointJson, _options);

		Assert.Equal(new Point(30, 10), geoPoint);
		Assert.Equal((30, 10), geoPoint);
	}

	[Fact(DisplayName = "GeoJSON Point serialization with altitude")]
	public void PointSerializationWithAltitude()
	{
		var geoPoint = new Point(30, 10, 5);

		var geoPointJson = JsonSerializer.Serialize(geoPoint, _options);

		Assert.Equal("""
		{
		  "type": "Point",
		  "coordinates": [
		    30,
		    10,
		    5
		  ]
		}
		""".ReplaceLineEndings(), geoPointJson.ReplaceLineEndings());
	}

	[Fact(DisplayName = "GeoJSON Point deserialization with altitude")]
	public void PointDeserializationWithAltitude()
	{
		var geoPointJson = """
		{
		  "type": "Point",
		  "coordinates": [
		    30,
		    10,
		    5
		  ]
		}
		"""u8;

		var geoPoint = JsonSerializer.Deserialize<Point>(geoPointJson, _options);

		Assert.Equal(new Point(30, 10, 5), geoPoint);
		Assert.Equal((30, 10, 5), geoPoint);
	}

	[Fact(DisplayName = "GeoJSON Point deserialization without altitude (short)")]
	public void PointDeserializationShortFormWithoutAltitude()
	{
		var geoPointJson = "[30, 10]"u8;

		var geoPoint = JsonSerializer.Deserialize<Point>(geoPointJson, _options);

		Assert.Equal(new Point(30, 10), geoPoint);
		Assert.Equal((30, 10), geoPoint);
	}


	[Fact(DisplayName = "GeoJSON Point deserialization with altitude (short)")]
	public void PointDeserializationShortFormWithAltitude()
	{
		var geoPointJson = "[30, 10, 5]"u8;

		var geoPoint = JsonSerializer.Deserialize<Point>(geoPointJson, _options);

		Assert.Equal(new Point(30, 10, 5), geoPoint);
		Assert.Equal((30, 10, 5), geoPoint);
	}
}
