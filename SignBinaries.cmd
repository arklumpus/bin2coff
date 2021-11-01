@echo off

if "%~1"=="" (
	echo.
	echo [91mThis tool requires the following parameters:[0m
	echo [91m    Path to P12/PFX certificate file[0m
	echo [91m    Certificate password[0m
	
	exit /b 64
)

if "%~2"=="" (
	echo.
	echo [91mThis tool requires the following parameters:[0m
	echo [91m    Path to P12/PFX certificate file[0m
	echo [91m    Certificate password[0m
	
	exit /b 64
)

if not exist %1 (
	echo.
	echo [91mThe specified certificate file does not exist![0m
	
	exit /b 1
)

echo.
echo [104;97mSigning binaries...[0m

signtool sign /fd sha256 /f %1 /p %2 /tr "http://ts.ssl.com" /td sha256 /v /a /d "bin2coff" /du "https://github.com/arklumpus/bin2coff" Release\win-x64\bin2coff.exe
signtool sign /fd sha256 /f %1 /p %2 /tr "http://ts.ssl.com" /td sha256 /v /a /d "bin2coff" /du "https://github.com/arklumpus/bin2coff" Release\win-x86\bin2coff.exe
signtool sign /fd sha256 /f %1 /p %2 /tr "http://ts.ssl.com" /td sha256 /v /a /d "bin2coff" /du "https://github.com/arklumpus/bin2coff" Release\win-arm64\bin2coff.exe

echo.
echo [94mAll done![0m
