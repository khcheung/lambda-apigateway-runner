#!/bin/bash
dotnet run
cd /opt/console
dotnet run --runtime linux-x64 --configuration "Release" --framework "net6.0" --self-contained --urls http://+:80
