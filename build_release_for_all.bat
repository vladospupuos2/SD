@echo false
set "PDIR=%~dp0"
cd /d "%PDIR%"

dotnet publish -c Release -r win-x86 --self-contained true
dotnet publish -c Release -r win-x64 --self-contained true

dotnet publish -c Release -r win-arm --self-contained true
dotnet publish -c Release -r win-arm64 --self-contained true

REM dotnet publish -c Release -r linux-x64 --self-contained true
REM dotnet publish -c Release -r linux-musl-x64 --self-contained true

REM dotnet publish -c Release -r linux-arm --self-contained true
REM dotnet publish -c Release -r linux-arm64 --self-contained true

REM dotnet publish -c Release -r osx-x64 --self-contained true
REM dotnet publish -c Release -r osx-arm64 --self-contained true

cd /d "%PDIR%/SD/bin/Release/net6.0/"

7z a "win-x86.zip" "win-x86"
7z a "win-x64.zip" "win-x64"
7z a "win-arm.zip" "win-arm"
7z a "win-arm64.zip" "win-arm64"

exit