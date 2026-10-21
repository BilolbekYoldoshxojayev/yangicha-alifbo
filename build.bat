@echo off
setlocal
echo ========================================================
echo       YangiAlifbo - Build and Test Runner
echo ========================================================

set "DOTNET_CMD=dotnet"
if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" (
    set "DOTNET_CMD=%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe"
)

echo Using .NET command: %DOTNET_CMD%

echo.
echo [1/2] Building Solution...
"%DOTNET_CMD%" build "YangiAlifbo.sln" -c Release
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    exit /b %ERRORLEVEL%
)

echo.
echo [2/2] Running Unit Tests...
"%DOTNET_CMD%" test "tests\YangiAlifbo.Tests.csproj" -c Release --verbosity normal
if %ERRORLEVEL% neq 0 (
    echo Tests failed!
    exit /b %ERRORLEVEL%
)

echo.
echo All builds and tests passed successfully!
endlocal
