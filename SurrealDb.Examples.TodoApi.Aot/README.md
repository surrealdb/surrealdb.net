# SurrealDb.Examples.TodoApi.Aot

A Minimal API project, compiled with Native AOT, that demonstrates how to use the .NET SDK for SurrealDB.

## Get started

First, start a new SurrealDB local instance:

```sh
surreal start --log debug --user root --pass root memory --allow-guests
```

Make sure that your SurrealDB server is running on `127.0.0.1:8000`, and that the `examples` namespace and the `aot` database exist.
Then publish the app and run it from the command line:

```sh
dotnet publish -c Release -r <runtime-identifier>
./bin/Release/net10.0/<runtime-identifier>/publish/SurrealDb.Examples.TodoApi.Aot
```

You can then get the data from the `/todos` API endpoint, for example `http://localhost:5000/todos`.

## Native AOT

Native AOT does not support the reflection that Dahomey.Cbor uses to serialize a type that has no registered converter.
The SDK registers converters for all of its own types, so you only register the converters of your models.

Register them with the `configureCborOptions` parameter of `AddSurreal` (or of the `SurrealDbClient` constructor).
The SDK calls it after it registers its own converters:

```csharp
services.AddSurreal(
    configuration.GetConnectionString("SurrealDB")!,
    ServiceLifetime.Singleton,
    configureCborOptions: AppCborMappings.Configure
);
```

[`AppCborMappings`](Models/AppCborMappings.cs) registers each model in the same way as a Dahomey.Cbor `CborSerializerContext`:

1. Register the converter of each member type that the SDK does not know, for example `DateOnly?`.
2. Register the object mapping of the model, with one `DelegateMemberMapping` for each member.
3. Register an `ObjectConverter<T>` that creates the model with a factory.

For the `Id` member of a `Record`, use the name `id` and ignore the default value, as `Record.Id` does.

### Source-generated context

Dahomey.Cbor can generate these registrations with a `CborSerializerContext`.
To use one, give the context a constructor that takes the options, and construct it in `configureCborOptions`:

```csharp
[CborSerializable(typeof(Todo))]
internal partial class AppCborContext : CborSerializerContext
{
    public AppCborContext(CborOptions options)
        : base(options) { }
}

services.AddSurreal(
    connectionString,
    configureCborOptions: options => new AppCborContext(options)
);
```

> [!NOTE]
> The Dahomey.Cbor 1.28.0 generator cannot generate a model that has a `RecordId` member or that derives from `Record`.
> It does not support the `[CborIgnoreIfDefault]` attribute on `Record.Id`,
> and it cannot use the `RecordId` converter that the SDK registers.
> Until it can, write the registrations by hand.
