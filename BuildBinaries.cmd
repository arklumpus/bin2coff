@echo off

echo.
echo [104;97mDeleting previous build...[0m

for /f %%i in ('dir /a:d /b Release\*') do rd /s /q Release\%%i
del Release\* /s /f /q 1>nul

echo.
echo Building with target [94mwin-x64[0m

cd bin2coff
dotnet publish -c Release /p:PublishProfile=Properties\PublishProfiles\win-x64.pubxml
cd ..

echo.
echo Building with target [94mwin-x86[0m

cd bin2coff
dotnet publish -c Release /p:PublishProfile=Properties\PublishProfiles\win-x86.pubxml
cd ..

echo.
echo Building with target [94mwin-arm64[0m

cd bin2coff
dotnet publish -c Release /p:PublishProfile=Properties\PublishProfiles\win-arm64.pubxml
cd ..

rm Release\win-x64\bin2coff.pdb
rm Release\win-x86\bin2coff.pdb
rm Release\win-arm64\bin2coff.pdb

echo.
echo [94mAll done![0m
