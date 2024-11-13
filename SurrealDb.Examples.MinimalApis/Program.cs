using Scalar.AspNetCore;
using SurrealDb.Examples.MinimalApis.Models;
using SurrealDb.Net;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

services
    .AddEndpointsApiExplorer()
    .AddOpenApi(options =>
    {
        options.AddDocumentTransformer(
            (document, _, _) =>
            {
                document.Info.Title = "MinimalApis Examples API";
                document.Info.Version = "v1";

                return Task.CompletedTask;
            }
        );
    });

// 💡 Be sure to have "NamingPolicy=CamelCase" in your connection string for PATCH methods to work as expected.
services.AddSurreal(configuration.GetConnectionString("SurrealDB")!);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGroup("/api")
    .MapSurrealEndpoints<WeatherForecast>(
        "/weatherForecast",
        new() { Tags = ["WeatherForecast"], EnableMutations = false }
    )
    .MapSurrealEndpoints<Todo>("/todo", new() { Tags = ["Todo"] });

if (app.Environment.IsDevelopment())
{
    // 💡 Enable OpenAPI document generation (e.g. "/openapi/v1.json")
    app.MapOpenApi();

    // 💡 Display OpenAPI User Interfaces (Swagger UI, Scalar)
    app.UseSwaggerUI(options =>
    {
        options.ConfigObject.Urls =
        [
            new UrlDescriptor { Name = "MinimalApis Examples API v1", Url = "/openapi/v1.json" }
        ];
    });
    app.MapScalarApiReference();
}

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

    var tasks = weatherForecasts.Select(weatherForecast =>
        surrealDbClient.Create(WeatherForecast.Table, weatherForecast)
    );

    await Task.WhenAll(tasks);
}
