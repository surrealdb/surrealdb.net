# SurrealDb.Examples.TodoApi.Aot

A Minimal API project (AoT compatible) example that demonstrates how to use the .NET SDK for SurrealDB.

## Get started

First, start a new SurrealDB local instance:

```sh
surreal start --log debug --user root --pass root memory --allow-guests
```

Then make sure your SurrealDB server is running on `127.0.0.1:8000` and run your app from the command line with:

```sh
dotnet run
```

You should be able to retrieve data from the following API endpoint: `https://localhost:5293/api/todos`.