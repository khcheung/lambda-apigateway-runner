#!/bin/bash
dotnet run
cd /opt/console
dotnet publish -o /opt/publish --runtime linux-x64 --configuration "Release" --framework "net6.0" --self-contained
cd /opt/publish
./console --urls http://+:80
