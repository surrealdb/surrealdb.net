# SurrealDb.Examples.Blazor.Server

A Blazor Server app example using the .NET SDK for SurrealDB.

This example provides examples on how to use the SurrealDB .NET SDK with a specific use case per web page.

* `/Counter` - Display & update a record counter
* `/FetchData` - Fetch and display a list of the recent weather forecasts
* `/LiveData` - Similar to the previous page, fetch and display a list of the recent weather forecasts in realtime via a Live Query
* `/RealtimeBoard` - A KANBAN board that supports drag'n'drop across columns, synchronized with Live Queries

## Get started

First, start a new SurrealDB local instance:

```sh
surreal start --log debug --user root --pass root memory --allow-guests
```

Then make sure your SurrealDB server is running on `127.0.0.1:8000` and run your app from the command line with:

```sh
dotnet run
```
