using System.Text;
using MudBlazor.Services;
using SurrealDb.Examples.Blazor.Server.Background;
using SurrealDb.Examples.Blazor.Server.Models;
using SurrealDb.Net;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

// Add services to the container.
services.AddRazorPages();
services.AddServerSideBlazor();
services.AddMudServices();
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
    await DefineSchemaAsync(serviceProvider);

    var tasks = new[]
    {
        GenerateWeatherForecastsAsync(serviceProvider),
        GenerateKanbanAsync(serviceProvider)
    };

    await Task.WhenAll(tasks);
}

async Task DefineSchemaAsync(IServiceProvider serviceProvider)
{
    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schema.surql");
    string schema = File.ReadAllText(filePath, Encoding.UTF8);

    var surrealDbClient = serviceProvider.GetRequiredService<ISurrealDbClient>();
    await surrealDbClient.RawQuery(schema);
}

async Task GenerateWeatherForecastsAsync(IServiceProvider serviceProvider)
{
    const int initialCount = 5;
    var weatherForecasts = new WeatherForecastFaker().Generate(initialCount);
    var surrealDbClient = serviceProvider.GetRequiredService<ISurrealDbClient>();

    var tasks = weatherForecasts.Select(
        weatherForecast => surrealDbClient.Create(WeatherForecast.Table, weatherForecast)
    );

    await Task.WhenAll(tasks);
}

async Task GenerateKanbanAsync(IServiceProvider serviceProvider)
{
    var surrealDbClient = serviceProvider.GetRequiredService<ISurrealDbClient>();

    var existingColumns = await surrealDbClient.Select<ColumnRecord>(ColumnRecord.Table);
    if (existingColumns.Any())
    {
        return;
    }

    var task1 = new TaskRecord { Title = "Create a new design", DueDate = DateTime.Now.AddDays(2) };
    var task2 = new TaskRecord { Title = "Finish the report", DueDate = DateTime.Now.AddDays(5) };
    var task3 = new TaskRecord
    {
        Title = "Update the project plan",
        DueDate = DateTime.Now.AddDays(3)
    };
    var task4 = new TaskRecord { Title = "Finish the proposal", DueDate = DateTime.Now.AddDays(2) };
    var task5 = new TaskRecord
    {
        Title = "Complete the presentation",
        DueDate = DateTime.Now.AddDays(5)
    };

    var taskTasks = new[] { task1, task2, task3, task4, task5 }.Select(
        t => surrealDbClient.Create(TaskRecord.Table, t)
    );

    var taskRecords = await Task.WhenAll(taskTasks);

    var todoColumn = new ColumnRecord
    {
        Name = "To Do",
        Order = 1,
        Tasks = new[] { taskRecords[0].Id!, taskRecords[1].Id! }
    };
    var inProgressColumn = new ColumnRecord
    {
        Name = "In Progress",
        Order = 2,
        Tasks = new[] { taskRecords[2].Id!, taskRecords[3].Id! }
    };
    var doneColumn = new ColumnRecord
    {
        Name = "Done",
        Order = 3,
        Tasks = new[] { taskRecords[4].Id! }
    };

    var columnTasks = new[] { todoColumn, inProgressColumn, doneColumn }.Select(
        column => surrealDbClient.Create(ColumnRecord.Table, column)
    );
    await Task.WhenAll(columnTasks);
}
