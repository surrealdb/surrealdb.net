#!/bin/bash

sed -i 's/PropertyGroup Label="Constants" Condition="true"/PropertyGroup Label="Constants" Condition="false"/' ./Directory.Build.props
exit 0