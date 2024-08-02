using System.Reflection;
using Microsoft.OpenApi.Models;
using SurrealDb.Examples.WeatherApi.Controllers;
using SurrealDb.Examples.WeatherApi.Models;
using SurrealDb.Net;

// 💡 Be careful, you should only enable this flag if you have the appropriate security measures in place based on the sensitivity of this data
const bool sensitiveDataLoggingEnabled = true;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WeatherForecast API", Version = "v1" });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
services.AddSurreal(
    SurrealDbOptions
        .Create()
        .FromConnectionString(configuration.GetConnectionString("SurrealDB")!)
        .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled)
        .Build()
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

await InitializeDbAsync();

app.Run();

async Task InitializeDbAsync()
{
    const int initialCount = 5;
    var weatherForecasts = new WeatherForecastFaker().Generate(initialCount);
    var surrealDbClient = new SurrealDbClient(
        SurrealDbOptions
            .Create()
            .FromConnectionString(configuration.GetConnectionString("SurrealDB")!)
            .EnableSensitiveDataLogging(sensitiveDataLoggingEnabled)
            .Build(),
        loggerFactory: LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        })
    );

    var tasks = weatherForecasts.Select(weatherForecast =>
        surrealDbClient.Create(WeatherForecastController.Table, weatherForecast)
    );

    await Task.WhenAll(tasks);
}
