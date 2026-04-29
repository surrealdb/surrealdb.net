# Get started

## Prerequisites

Before contributing to this repository, please take note of the [Contributing](./CONTRIBUTING.md) guidelines. To contribute to this project, you will also need to install the following tools:

- The .NET SDK, preferably the latest stable version which is available for [download here](https://dotnet.microsoft.com/download)
- The [Rust programming language](https://www.rust-lang.org/learn/get-started), in order to build the embedded providers

### Embedded mode

The test and benchmark projects are dependent on the local Rust crate used by embedded providers. This crate is located in the [./rust-embedded](./rust-embedded) folder of this repository. To build the crate, make sure you installed the Rust toolchain on your machine and then follow these steps:

```sh
cd ./rust-embedded
cargo build
```

If the command line was successful, the compiled libraries are generated in the target folder and automatically copied when the .NET projects are built.

Note: you can manually disable the embedded mode by changing the value of the constant `EMBEDDED_MODE` located in the `Directory.Build.props` file like this:

```xml
<PropertyGroup Label="Constants" Condition="false">
  <DefineConstants>EMBEDDED_MODE</DefineConstants>
</PropertyGroup>
```

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

Unit/Integration tests are written using [TUnit](https://thomhurst.github.io/TUnit/) and [FluentAssertions](https://fluentassertions.com/).

You will need a local SurrealDB instance alongside the tests. Start one using the following command:

```sh
surreal start --log debug --user root --pass root memory --allow-guests
```

Once ready, go to the root directory of the project and run the following command:

```sh
dotnet watch test --project SurrealDb.Net.Tests
```

Due to the asynchronous nature of Live Queries, they are tested against a separate project named `SurrealDb.Net.LiveQuery.Tests`. Where the default test project allow full parallelization, this project completely disable test parallelization. To execute tests on Live Queries, run the following command:

```sh
dotnet watch test --project SurrealDb.Net.LiveQuery.Tests
```

Note 1: Because Live Query tests are not run in parallel, it can take quite some time to run all tests.

Note 2: You can run the two test projects in parallel.

## Benchmarking

This project also contains [benchmarks](https://benchmarkdotnet.org/) in order to detect possible performance regressions.

You will need a local SurrealDB instance alongside the tests. Start one using the following command:

```sh
surreal start --user root --pass root memory --allow-guests
```

Once ready, go to the root directory of the project and run the following command:

```sh
dotnet run -c Release --project SurrealDb.Net.Benchmarks.Remote --filter '*'
```

```sh
./prepare_embedded_benchmarks.sh -s
dotnet run -c Release --project SurrealDb.Net.Benchmarks.Embedded --filter '*'
./prepare_embedded_benchmarks.sh -e
```

For Windows:

```sh
./prepare_embedded_benchmarks.ps1 -s
dotnet run -c Release --project SurrealDb.Net.Benchmarks.Embedded --filter '*'
./prepare_embedded_benchmarks.ps1 -e
```
