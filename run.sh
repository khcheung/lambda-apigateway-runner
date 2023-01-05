#!/bin/bash
dotnet run
cd /opt/console
#dotnet run -r linux-x64 -c Release --no-self-contained --urls "http://+:80"
#dotnet run -r linux-x64 -c Release --sc --urls "http://+:80"
dotnet run --runtime linux-x64 --configuration "Release" --framework "net6.0" --self-contained false --urls "http://+:80"   
