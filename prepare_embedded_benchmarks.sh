#!/bin/bash

if [ "$1" == "-s" ]; then
    sed -i 's/PropertyGroup Label="Constants" Condition="false"/PropertyGroup Label="Constants" Condition="true"/' ./SurrealDb.Embedded.InMemory/SurrealDb.Embedded.InMemory.csproj
    exit 0
fi

if [ "$1" == "-e" ]; then
    sed -i 's/PropertyGroup Label="Constants" Condition="true"/PropertyGroup Label="Constants" Condition="false"/' ./SurrealDb.Embedded.InMemory/SurrealDb.Embedded.InMemory.csproj
    exit 0
fi

exit 1
