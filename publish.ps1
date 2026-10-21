$ErrorActionPreference = "Stop"
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "    YangiAlifbo — Self-Contained Build   " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

$dotnetCmd = "dotnet"
$localDotnet = "$env:LocalAppData\Microsoft\dotnet\dotnet.exe"
if (Test-Path $localDotnet) {
    $dotnetCmd = $localDotnet
    Write-Host "Using user-local .NET SDK: $dotnetCmd" -ForegroundColor Green
}

Write-Host "`n[1/3] Running Automated Tests..." -ForegroundColor Yellow
& $dotnetCmd test (Join-Path $PSScriptRoot "tests\YangiAlifbo.Tests.csproj") -c Release --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed! Aborting publish." -ForegroundColor Red
    exit 1
}

Write-Host "`n[2/3] Publishing single-file executable..." -ForegroundColor Yellow
$outDir = Join-Path $PSScriptRoot "publish"

& $dotnetCmd publish (Join-Path $PSScriptRoot "src\YangiAlifbo.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $outDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`n[3/3] Verifying Output..." -ForegroundColor Yellow
$exe = Join-Path $outDir 'YangiAlifbo.exe'
if (Test-Path $exe) {
    $len = (Get-Item $exe).Length
    Write-Host "SUCCESS: Executable generated at $exe ($len bytes)" -ForegroundColor Green
} else {
    Write-Host "ERROR: Executable not found" -ForegroundColor Red
    exit 1
}