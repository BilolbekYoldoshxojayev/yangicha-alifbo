@echo off
setlocal
echo ========================================================
echo        YangiAlifbo - E2E Test Suite Runner (Tiers 1-4)
echo ========================================================

set "DOTNET_CMD=dotnet"
if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" (
    set "DOTNET_CMD=%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe"
)

echo Using .NET command: %DOTNET_CMD%

set "PROJECT_PATH=%~dp0YangiAlifbo.E2ETests.csproj"

echo.
echo [1/2] Building E2E Test Suite...
"%DOTNET_CMD%" build "%PROJECT_PATH%" -c Release
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    exit /b %ERRORLEVEL%
)

echo.
echo [2/2] Running E2E Test Suite...
"%DOTNET_CMD%" test "%PROJECT_PATH%" -c Release --verbosity normal
if %ERRORLEVEL% neq 0 (
    echo E2E Tests failed!
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================================
echo   All 270 E2E tests passed successfully! (Exit Code 0)
echo ========================================================
endlocal
exit /b 0
