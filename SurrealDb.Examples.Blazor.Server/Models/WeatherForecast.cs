using SurrealDb.Net.Models;

namespace SurrealDb.Examples.Blazor.Server.Models;

public class WeatherForecast : Record
{
    internal const string Table = "weatherForecast";

    public DateTime Date { get; set; }
    public string? Country { get; set; }
    public int TemperatureC { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    public string? Summary { get; set; }
}
