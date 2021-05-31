#!/usr/bin/env bash

killall thorn
dotnet publish -c Release
./thorn/bin/Release/net5.0/publish/thorn