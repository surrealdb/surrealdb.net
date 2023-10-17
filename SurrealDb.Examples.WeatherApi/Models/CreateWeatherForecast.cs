namespace SurrealDb.Examples.WeatherApi.Models;

/// <summary>
/// Weather forecast model for Create endpoint.
/// </summary>
public class CreateWeatherForecast
{
    /// <summary>
    /// Date of the weather forecast.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Country of the weather forecast.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Temperature in Celsius.
    /// </summary>
    public int TemperatureC { get; set; }

    /// <summary>
    /// Summary of the weather forecast.
    /// </summary>
    public string? Summary { get; set; }
}
