@echo off
set baseDir=%cd%
set configuration=Release

msbuild %baseDir%\src\Overseer.WebApp\Overseer.WebApp.csproj /p:Configuration=%Configuration% /p:PublishProfile=localhost /p:DeployOnBuild=true

rmdir /q /s %baseDir%\bin
mkdir %baseDir%\bin
msbuild %baseDir%\src\Overseer.Doorkeeper\Overseer.Doorkeeper.csproj /p:Configuration=%Configuration% /p:OutputPath=%baseDir%\bin /v:minimal /nologo
%baseDir%\bin\Overseer.Doorkeeper.exe
