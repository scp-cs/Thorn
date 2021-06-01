#!/usr/bin/env bash

killall thorn
dotnet publish -c Release
nohup thorn/bin/Release/net5.0/publish/thorn &