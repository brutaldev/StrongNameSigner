@echo off

cls

pushd .
call "C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\Tools\VsMSBuildCmd.bat"
popd

dotnet restore "..\Brutal.Dev.StrongNameSigner.slnx"
dotnet publish -c Release "..\Brutal.Dev.StrongNameSigner\Brutal.Dev.StrongNameSigner.csproj"

MSBuild "..\Brutal.Dev.StrongNameSigner.slnx" /t:Rebuild /p:Configuration=Release /m

echo.
if not exist "%NUGET_PACKAGES%\tools.innosetup.5.6.1\tools\ISCC.exe" (
    echo Downloading Tools.InnoSetup 5.6.1...
    curl -L -o "%TEMP%\tools.innosetup.5.6.1.zip" "https://www.nuget.org/api/v2/package/Tools.InnoSetup/5.6.1"
    powershell -Command "Expand-Archive -Path \"$env:TEMP\tools.innosetup.5.6.1.zip\" -DestinationPath \"$env:NUGET_PACKAGES\tools.innosetup.5.6.1\" -Force"
    del "%TEMP%\tools.innosetup.5.6.1.zip"
)
"%NUGET_PACKAGES%\tools.innosetup.5.6.1\tools\ISCC.exe" StrongNameSigner.iss
echo.

echo.
"..\.nuget\NuGet.exe" pack StrongNameSigner.nuspec
move *.nupkg ..\..\deploy\
echo.

pause
