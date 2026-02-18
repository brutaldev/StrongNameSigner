@echo off

cls

pushd .
call "C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\Tools\VsMSBuildCmd.bat"
popd

dotnet restore "..\Brutal.Dev.StrongNameSigner.slnx"
dotnet publish -c Release "..\Brutal.Dev.StrongNameSigner\Brutal.Dev.StrongNameSigner.csproj"

MSBuild "..\Brutal.Dev.StrongNameSigner.slnx" /t:Rebuild /p:Configuration=Release /m

echo.
"%NUGET_PACKAGES%\tools.innosetup.5.6.1\tools\ISCC.exe" StrongNameSigner.iss
echo.

echo.
"..\.nuget\NuGet.exe" pack StrongNameSigner.nuspec
move *.nupkg ..\..\deploy\
echo.

pause
