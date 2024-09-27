# SurrealDb.Net.Benchmarks.Embedded

A set of benchmarks to display performance of the SurrealDb.Net SDK using remote engines (HTTP, WS).

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

```sh
dotnet run -c Release --project SurrealDb.Net.Benchmarks.Remote --filter '*'
```
