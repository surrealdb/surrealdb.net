using SurrealDb.Examples.Blazor.Server.Models;
using SurrealDb.Net;

namespace SurrealDb.Examples.Blazor.Server.Background;

public class WeatherForecastHostedService : BackgroundService
{
    private readonly ILogger<WeatherForecastHostedService> _logger;
    private readonly ISurrealDbClient _surrealDbClient;
    private readonly WeatherForecastFaker _weatherForecastFaker = new();

    public WeatherForecastHostedService(
        ILogger<WeatherForecastHostedService> logger,
        ISurrealDbClient surrealDbClient
    )
    {
        _logger = logger;
        _surrealDbClient = surrealDbClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"{nameof(WeatherForecastHostedService)} is running.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await DoWorkAsync();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation($"{nameof(WeatherForecastHostedService)} is stopping.");
        }
    }

    private async Task DoWorkAsync()
    {
        var weatherForecast = _weatherForecastFaker.Generate(1).Single();
        weatherForecast.Date = DateTime.Now;

        await _surrealDbClient.Create(WeatherForecast.Table, weatherForecast);
    }
}
