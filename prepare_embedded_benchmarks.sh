#!/bin/bash

if [ "$1" == "-s" ]; then
    sed -i 's/PropertyGroup Label="Constants" Condition="false"/PropertyGroup Label="Constants" Condition="true"/' ./SurrealDb.Embedded.InMemory/SurrealDb.Embedded.InMemory.csproj
    sed -i 's/PropertyGroup Label="Constants" Condition="false"/PropertyGroup Label="Constants" Condition="true"/' ./SurrealDb.Embedded.RocksDb/SurrealDb.Embedded.RocksDb.csproj
    sed -i 's/PropertyGroup Label="Constants" Condition="false"/PropertyGroup Label="Constants" Condition="true"/' ./SurrealDb.Embedded.SurrealKv/SurrealDb.Embedded.SurrealKv.csproj
    exit 0
fi

if [ "$1" == "-e" ]; then
    sed -i 's/PropertyGroup Label="Constants" Condition="true"/PropertyGroup Label="Constants" Condition="false"/' ./SurrealDb.Embedded.InMemory/SurrealDb.Embedded.InMemory.csproj
    sed -i 's/PropertyGroup Label="Constants" Condition="true"/PropertyGroup Label="Constants" Condition="false"/' ./SurrealDb.Embedded.RocksDb/SurrealDb.Embedded.RocksDb.csproj
    sed -i 's/PropertyGroup Label="Constants" Condition="true"/PropertyGroup Label="Constants" Condition="false"/' ./SurrealDb.Embedded.SurrealKv/SurrealDb.Embedded.SurrealKv.csproj
    exit 0
fi

exit 1
