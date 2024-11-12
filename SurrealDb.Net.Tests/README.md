# Contributing

## Formatting

This project is using [CSharpier](https://csharpier.com/), an opinionated code formatter.

### Command line

You can install it on your machine via `dotnet tool`.

```sh
# Run this command at the root of the project
dotnet tool install csharpier
```

You can then use it as a cli:

```sh
dotnet csharpier .
```

The list of command-line options is available here: https://csharpier.com/docs/CLI

### IDE integration

CSharpier supports [multiple code editors](https://csharpier.com/docs/Editors), including Visual Studio, Jetbrains Rider, VSCode and Neovim. You will be able to run format on file save after configuring the settings in your IDE. 

## Testing

This project was written following testing best practices:

- TDD, leveraging:
  - clean code/architecture
  - regression testing
  - adding new features and tests easily
- a vast majority of tests are integration tests, ensuring compatibility with a concrete SurrealDB version
- each integration test is using a separate SurrealDB namespace/database

Unit/Integration tests are written using [xUnit](https://xunit.net/) and [FluentAssertions](https://fluentassertions.com/).

You will need a local SurrealDB instance alongside the tests. Start one using the following command:

```sh
surreal start --log debug --user root --pass root memory --allow-guests
```

Once ready, go to the root directory of the project and run the following command:

```sh
dotnet watch test --project SurrealDb.Net.Tests
```
