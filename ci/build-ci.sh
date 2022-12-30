#!/bin/bash

sed -i -e 's/\/Applications\/Unity\/Editor\/2021.3.16f1\/Unity.app\/Contents/\/opt\/Unity\/Editor\/Data\/Managed\/UnityEngine/g' PeacefulPie/PeacefulPie.csproj
dotnet build PeacefulPie &&  \
    cp PeacefulPie/bin/Debug/netstandard2.1/PeacefulPie.dll .
