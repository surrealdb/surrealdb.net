using Microsoft.OpenApi.Models;
using SurrealDb.Examples.MinimalApis.Models;
using SurrealDb.Net;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MinimalApis Examples API", Version = "v1" });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
services.AddSurreal(configuration.GetConnectionString("SurrealDB")!);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet(
    "/api/weatherForecast",
    (ISurrealDbClient surrealDbClient, CancellationToken cancellationToken) =>
        surrealDbClient.Select<WeatherForecast>(WeatherForecast.Table, cancellationToken)
);

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
            .Build()
    );

    var tasks = weatherForecasts.Select(
        weatherForecast => surrealDbClient.Create(WeatherForecast.Table, weatherForecast)
    );

    await Task.WhenAll(tasks);
}
