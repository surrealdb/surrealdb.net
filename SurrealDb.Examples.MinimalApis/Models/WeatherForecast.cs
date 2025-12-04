using System.ComponentModel.DataAnnotations.Schema;
using Dahomey.Cbor.Attributes;
using SurrealDb.Net.Json;
using SurrealDb.Net.Models;

namespace SurrealDb.Examples.MinimalApis.Models;

/// <summary>
/// Weather forecast model.
/// </summary>
public class WeatherForecast : IRecord
{
    internal const string Table = "weatherForecast";

    [RecordIdJsonConverter(Table)]
    [CborProperty("id")]
    [CborIgnoreIfDefault]
    public RecordId? Id { get; set; }

    /// <summary>
    /// Date of the weather forecast.
    /// </summary>
    [Column("date")]
    public DateTime Date { get; set; }

    /// <summary>
    /// Country of the weather forecast.
    /// </summary>
    [Column("country")]
    public string? Country { get; set; }

    /// <summary>
    /// Temperature in Celsius.
    /// </summary>
    [Column("temperatureC")]
    public int TemperatureC { get; set; }

    /// <summary>
    /// Temperature in Fahrenheit.
    /// </summary>
    [Column("temperatureF")]
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    /// <summary>
    /// Summary of the weather forecast.
    /// </summary>
    [Column("summary")]
    public string? Summary { get; set; }
}
