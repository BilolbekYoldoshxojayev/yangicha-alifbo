# WORKFLOW — YangiAlifbo

## Build Commands
```powershell
# Build solution in Release configuration
dotnet build YangiAlifbo.sln -c Release
```

## Test Commands
```powershell
# Run all unit tests
dotnet test tests/YangiAlifbo.Tests.csproj -c Release --verbosity normal
```

## Publish Command
```powershell
# Generate self-contained single-file binary
.\publish.ps1
```
Output: `c:\dev\Projects\alphabet\YangiAlifbo\publish\YangiAlifbo.exe`
