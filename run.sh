#!/bin/bash
dotnet run
cd /opt/console
#dotnet run -r linux-x64 -c Release --no-self-contained --urls "http://+:80"
dotnet run -r linux-x64 -c Release --sc --urls "http://+:80"
