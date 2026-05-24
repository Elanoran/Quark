$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

dotnet build .\Quark.sln --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet .\src\Quark.App\bin\Debug\net8.0-windows\Quark.dll --screenshot .\assets\screenshot-settings.png
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
