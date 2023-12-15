using SurrealDb.Examples.Blazor.Server.Background;
using SurrealDb.Examples.Blazor.Server.Models;
using SurrealDb.Net;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

// Add services to the container.
services.AddRazorPages();
services.AddServerSideBlazor();
services.AddSurreal(configuration.GetConnectionString("SurrealDB")!);
services.AddHostedService<WeatherForecastHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

_ = Task.Run(async () =>
{
    await InitializeDbAsync(app.Services);
});

app.Run();

async Task InitializeDbAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();

    const int initialCount = 5;
    var weatherForecasts = new WeatherForecastFaker().Generate(initialCount);
    var surrealDbClient = scope.ServiceProvider.GetRequiredService<ISurrealDbClient>();

    var tasks = weatherForecasts.Select(
        weatherForecast => surrealDbClient.Create(WeatherForecast.Table, weatherForecast)
    );

    await Task.WhenAll(tasks);
}
