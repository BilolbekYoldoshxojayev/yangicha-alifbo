# ──────────────────────────────────────────────────────────────────────
# YangiAlifbo E2E Test Suite Runner (PowerShell)
# ──────────────────────────────────────────────────────────────────────

$ErrorActionPreference = "Stop"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "       YangiAlifbo - E2E Test Suite Runner (Tiers 1-4)  " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

$dotnetCmd = "dotnet"
$localDotnet = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet\dotnet.exe"
if (Test-Path $localDotnet) {
    $dotnetCmd = $localDotnet
}

Write-Host "Using .NET command: $dotnetCmd" -ForegroundColor Gray

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptDir "YangiAlifbo.E2ETests.csproj"

Write-Host "`n[1/2] Building E2E Test Suite..." -ForegroundColor Yellow
& $dotnetCmd build $projectPath -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed with exit code $LASTEXITCODE!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "`n[2/2] Running E2E Test Suite (Tiers 1-4)..." -ForegroundColor Yellow
& $dotnetCmd test $projectPath -c Release --verbosity normal --logger "console;verbosity=detailed"
if ($LASTEXITCODE -ne 0) {
    Write-Host "E2E Tests failed with exit code $LASTEXITCODE!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host "  All 270 E2E tests passed successfully! (Exit Code 0)  " -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green

exit 0
