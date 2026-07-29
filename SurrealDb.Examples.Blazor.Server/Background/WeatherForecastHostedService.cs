using SurrealDb.Examples.Blazor.Server.Models;
using SurrealDb.Net;

namespace SurrealDb.Examples.Blazor.Server.Background;

public class WeatherForecastHostedService : BackgroundService
{
    private readonly ILogger<WeatherForecastHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly WeatherForecastFaker _weatherForecastFaker = new();

    public WeatherForecastHostedService(
        ILogger<WeatherForecastHostedService> logger,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
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

        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var db = scope.ServiceProvider.GetRequiredService<ISurrealDbSession>();

        await db.Create(WeatherForecast.Table, weatherForecast);
    }
}
