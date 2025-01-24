using Bogus;

namespace SurrealDb.Examples.Blazor.Server.Models;

public sealed class WeatherForecastFaker : Faker<WeatherForecast>
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing",
        "Bracing",
        "Chilly",
        "Cool",
        "Mild",
        "Warm",
        "Balmy",
        "Hot",
        "Sweltering",
        "Scorching",
    };

    public WeatherForecastFaker()
    {
        RuleFor(o => o.Date, f => f.Date.Recent());
        RuleFor(o => o.Country, f => f.Address.Country());
        RuleFor(o => o.TemperatureC, f => f.Random.Number(-20, 55));
        RuleFor(o => o.Summary, f => f.Random.ArrayElement(Summaries));
    }
}
