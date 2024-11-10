if ($args[0] -eq "-s") {
    (Get-Content -Path .\SurrealDb.Embedded.InMemory\SurrealDb.Embedded.InMemory.csproj) -replace 'PropertyGroup Label="Constants" Condition="false"', 'PropertyGroup Label="Constants" Condition="true"' | Set-Content -Path .\SurrealDb.Embedded.InMemory\SurrealDb.Embedded.InMemory.csproj
    (Get-Content -Path .\SurrealDb.Embedded.RocksDb\SurrealDb.Embedded.RocksDb.csproj) -replace 'PropertyGroup Label="Constants" Condition="false"', 'PropertyGroup Label="Constants" Condition="true"' | Set-Content -Path .\SurrealDb.Embedded.RocksDb\SurrealDb.Embedded.RocksDb.csproj
    (Get-Content -Path .\SurrealDb.Embedded.SurrealKv\SurrealDb.Embedded.SurrealKv.csproj) -replace 'PropertyGroup Label="Constants" Condition="false"', 'PropertyGroup Label="Constants" Condition="true"' | Set-Content -Path .\SurrealDb.Embedded.SurrealKv\SurrealDb.Embedded.SurrealKv.csproj
    exit 0
}

if ($args[0] -eq "-e") {
    (Get-Content -Path .\SurrealDb.Embedded.InMemory\SurrealDb.Embedded.InMemory.csproj) -replace 'PropertyGroup Label="Constants" Condition="true"', 'PropertyGroup Label="Constants" Condition="false"' | Set-Content -Path .\SurrealDb.Embedded.InMemory\SurrealDb.Embedded.InMemory.csproj
    (Get-Content -Path .\SurrealDb.Embedded.RocksDb\SurrealDb.Embedded.RocksDb.csproj) -replace 'PropertyGroup Label="Constants" Condition="true"', 'PropertyGroup Label="Constants" Condition="false"' | Set-Content -Path .\SurrealDb.Embedded.RocksDb\SurrealDb.Embedded.RocksDb.csproj
    (Get-Content -Path .\SurrealDb.Embedded.SurrealKv\SurrealDb.Embedded.SurrealKv.csproj) -replace 'PropertyGroup Label="Constants" Condition="true"', 'PropertyGroup Label="Constants" Condition="false"' | Set-Content -Path .\SurrealDb.Embedded.SurrealKv\SurrealDb.Embedded.SurrealKv.csproj
    exit 0
}

exit 1