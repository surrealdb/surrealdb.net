﻿using Microsoft.OpenApi.Models;
using SurrealDb.Net;
using SurrealDb.Examples.WeatherApi.Controllers;
using SurrealDb.Examples.WeatherApi.Models;
using System.Reflection;

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
services.AddSurreal(configuration.GetConnectionString("SurrealDB")!);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();

await InitializeDbAsync(app.Services);

async Task InitializeDbAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();

    int initialCount = 5;
    var weatherForecasts = new WeatherForecastFaker().Generate(initialCount);
    var surrealDbClient = scope.ServiceProvider.GetRequiredService<ISurrealDbClient>();

    var tasks = weatherForecasts.Select(
        weatherForecast => surrealDbClient.Create(WeatherForecastController.Table, weatherForecast)
    );

    await Task.WhenAll(tasks);
}
