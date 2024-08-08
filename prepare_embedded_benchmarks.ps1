if ($args[0] -eq "-s") {
    (Get-Content -Path .\SurrealDb.Embedded.InMemory\SurrealDb.Embedded.InMemory.csproj) -replace 'PropertyGroup Label="Constants" Condition="false"', 'PropertyGroup Label="Constants" Condition="true"' | Set-Content -Path .\SurrealDb.Embedded.InMemory\SurrealDb.Embedded.InMemory.csproj
    exit 0
}

if ($args[0] -eq "-e") {
    (Get-Content -Path .\SurrealDb.Embedded.InMemory\SurrealDb.Embedded.InMemory.csproj) -replace 'PropertyGroup Label="Constants" Condition="true"', 'PropertyGroup Label="Constants" Condition="false"' | Set-Content -Path .\SurrealDb.Embedded.InMemory\SurrealDb.Embedded.InMemory.csproj
    exit 0
}

exit 1