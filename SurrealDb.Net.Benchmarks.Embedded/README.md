# SurrealDb.Net.Benchmarks.Embedded

A set of benchmarks to display performance of the SurrealDb.Net SDK using embedded modes (In-Memory, File, IndexedDb).

Here is the list of benchmark currently written, sorted by complexity (simplest to more complex):

| Name       | Description                                                                                              |
| ---------- | -------------------------------------------------------------------------------------------------------- |
| Cold Start | Creates a new `SurrealDbClient` and connects to the specific engine                                      |
| Select     | Select all records from `post` table                                                                     |
| Create     | Creates a single record in the `post` table                                                              |
| Delete     | Deletes the whole `post` table previously generated with 1000 records                                    |
| Upsert     | Updates the first record inside the `post` table                                                         |
| Query      | Custom query that selects from 2 tables and then execute a canceled transaction                          |
| Scenario   | Executes a complex scenario across multiple tables (record creation, record relations and complex query) |

## Get started

You will need a local SurrealDB instance alongside the tests. Start one using the following command:

```sh
surreal start --user root --pass root memory --allow-guests
```

Once ready, go to the root directory of the project and run the following command:

## Unix

```sh
./prepare_embedded_benchmarks.sh -s
dotnet run -c Release --project SurrealDb.Net.Benchmarks.Embedded --filter '*'
./prepare_embedded_benchmarks.sh -e
```

### Windows

```sh
./prepare_embedded_benchmarks.ps1 -s
dotnet run -c Release --project SurrealDb.Net.Benchmarks.Embedded --filter '*'
./prepare_embedded_benchmarks.ps1 -e
```
