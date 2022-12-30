#!/bin/bash

set -x

sed -i -e 's/\/Applications\/Unity\/Hub\/Editor\/2021.3.16f1\/Unity.app\/Contents\/Managed\/UnityEngine/\/opt\/Unity\/Editor\/Data\/Managed\/UnityEngine/g' PeacefulPie/PeacefulPie.csproj
cat PeacefulPie/PeacefulPie.csproj
dotnet build PeacefulPie &&  \
    cp PeacefulPie/bin/Debug/netstandard2.1/PeacefulPie.dll .
find . -cmin -1
find . -cmin -1 -name 'PeacefulPie*'
