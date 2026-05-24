$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

& .\scripts\generate-icon.ps1

dotnet restore .\src\Quark.App\Quark.App.csproj `
  -r win-x64 `
  --configfile .\NuGet.Config
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet publish .\src\Quark.App\Quark.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
