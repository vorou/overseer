@echo off
set baseDir=%cd%
rmdir /q /s %baseDir%\bin
mkdir %baseDir%\bin
msbuild %baseDir%\src\Overseer.Doorkeeper\Overseer.Doorkeeper.csproj /p:Configuration=Release /p:OutputPath=%baseDir%\bin /v:minimal /nologo
