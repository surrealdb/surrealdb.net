#!/bin/bash

if [ "$1" == "-s" ]; then
    sed -i 's/PropertyGroup Label="Constants" Condition="false"/PropertyGroup Label="Constants" Condition="true"/' ./SurrealDb.Embedded.InMemory/SurrealDb.Embedded.InMemory.csproj
    sed -i 's/PropertyGroup Label="Constants" Condition="false"/PropertyGroup Label="Constants" Condition="true"/' ./SurrealDb.Embedded.RockDb/SurrealDb.Embedded.RocksDb.csproj
    exit 0
fi

if [ "$1" == "-e" ]; then
    sed -i 's/PropertyGroup Label="Constants" Condition="true"/PropertyGroup Label="Constants" Condition="false"/' ./SurrealDb.Embedded.InMemory/SurrealDb.Embedded.InMemory.csproj
    sed -i 's/PropertyGroup Label="Constants" Condition="true"/PropertyGroup Label="Constants" Condition="false"/' ./SurrealDb.Embedded.RockDb/SurrealDb.Embedded.RockDb.csproj
    exit 0
fi

exit 1
