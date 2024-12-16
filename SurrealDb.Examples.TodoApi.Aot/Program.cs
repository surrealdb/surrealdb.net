using System.Text.Json.Serialization;
using SurrealDb.Examples.TodoApi.Aot.Models;
using SurrealDb.Net;

// TODO : Handle AOT via CborSerializerContext

HandleTimeoutFromArgs(args);

var builder = WebApplication.CreateSlimBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

JsonSerializerContext[] jsonSerializerContexts = { AppJsonSerializerContext.Default };
services.AddSurreal(configuration.GetConnectionString("SurrealDB")!);

var app = builder.Build();

var todosApi = app.MapGroup("/todos");

todosApi.MapGet(
    "/",
    (ISurrealDbClient surrealDbClient, CancellationToken cancellationToken) =>
        surrealDbClient.Select<Todo>(Todo.Table, cancellationToken)
);
todosApi.MapGet(
    "/{id:int}",
    async (int id, ISurrealDbClient surrealDbClient, CancellationToken cancellationToken) =>
    {
        var todo = await surrealDbClient.Select<Todo>(
            (Todo.Table, id.ToString()),
            cancellationToken
        );
        return todo is not null ? Results.Ok(todo) : Results.NotFound();
    }
);

await InitializeDbAsync();

app.Run();

async Task InitializeDbAsync()
{
    var sampleTodos = new Todo[]
    {
        new(1, "Walk the dog"),
        new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
        new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
        new(4, "Clean the bathroom"),
        new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
    };

    var surrealDbClient = new SurrealDbClient(
        SurrealDbOptions
            .Create()
            .FromConnectionString(configuration.GetConnectionString("SurrealDB")!)
            .Build()
    );

    var tasks = sampleTodos.Select(async todo =>
    {
        await surrealDbClient.Upsert(todo);
    });

    await Task.WhenAll(tasks);
}

void HandleTimeoutFromArgs(string[] args)
{
    if (args.Length > 0)
    {
        int timeout = int.TryParse(args[0], out int value) ? value : 0;
        if (timeout > 0)
        {
            Console.WriteLine($"Application will be killed in {timeout}ms");

            _ = Task.Run(async () =>
            {
                await Task.Delay(timeout);
                Environment.Exit(0);
            });
        }
    }
}

[JsonSerializable(typeof(IEnumerable<Todo>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
