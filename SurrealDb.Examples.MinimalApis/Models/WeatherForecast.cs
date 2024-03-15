﻿using SurrealDb.Net.Models;

namespace SurrealDb.Examples.MinimalApis.Models;

/// <summary>
/// Weather forecast model.
/// </summary>
public class WeatherForecast : Record
{
    internal const string Table = "weather_forecast";

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
    /// Temperature in Fahrenheit.
    /// </summary>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    /// <summary>
    /// Summary of the weather forecast.
    /// </summary>
    public string? Summary { get; set; }
}
