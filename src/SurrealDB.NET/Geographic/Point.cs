namespace SurrealDB.NET.Geographic;

public readonly record struct Point
{
	public double Longitude { get; }
	public double Latitude { get; }
	public double? Altitude { get; }

	public Point(double longitude, double latitude, double? altitude = null)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(longitude, -180);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(longitude, 180);

		ArgumentOutOfRangeException.ThrowIfLessThan(latitude, -90);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(latitude, 90);

		if (altitude is double.NaN)
			throw new ArgumentOutOfRangeException(nameof(altitude), "The altitude cannot be NaN");

		if (altitude is not null && double.IsInfinity(altitude.Value))
			throw new ArgumentOutOfRangeException(nameof(altitude), "The altitude cannot be infinity");

		Longitude = longitude;
		Latitude = latitude;
		Altitude = altitude;
	}

	public static implicit operator Point((double longitude, double latitude) coordinates)
		=> ToPoint(coordinates);

	public static implicit operator Point((double longitude, double latitude, double altitude) coordinates)
		=> ToPoint(coordinates);

	public static Point ToPoint((double longitude, double latitude) coordinates)
		=> new(coordinates.longitude, coordinates.latitude);

	public static Point ToPoint((double longitude, double latitude, double altitude) coordinates)
	=> new(coordinates.longitude, coordinates.latitude, coordinates.altitude);
}
