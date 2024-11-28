# SurrealDb.Examples.WeatherApi

An ASP.NET API project example that demonstrates how to use the .NET SDK for SurrealDB within API Controllers.

## Get started

First, start a new SurrealDB local instance:

```sh
surreal start --log debug --user root --pass root memory --allow-guests
```

Then make sure your SurrealDB server is running on `127.0.0.1:8000` and run your app from the command line with:

```sh
dotnet run
```

This will open a Swagger UI accessible at `https://localhost:7142/swagger`.