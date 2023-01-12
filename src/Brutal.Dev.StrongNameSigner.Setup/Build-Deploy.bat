@echo off

cls

pushd .
call "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsMSBuildCmd.bat"
popd

dotnet restore -c Release 
dotnet publish -c Release "..\Brutal.Dev.StrongNameSigner\Brutal.Dev.StrongNameSigner.csproj"

MSBuild "..\Brutal.Dev.StrongNameSigner.sln" /t:Rebuild /p:Configuration=Release /m

echo.
"..\..\packages\Tools.InnoSetup.5.6.1\tools\ISCC.exe" StrongNameSigner.iss
echo.

echo.
"..\.nuget\NuGet.exe" pack StrongNameSigner.nuspec
move *.nupkg ..\..\deploy\
echo.

pause
