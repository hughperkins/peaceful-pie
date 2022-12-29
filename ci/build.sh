#!/bin/bash

dotnet build PeacefulPie &&  \
    cp PeacefulPie/bin/Debug/netstandard2.1/PeacefulPie.dll .
