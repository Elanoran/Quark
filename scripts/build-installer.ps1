$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

& .\scripts\publish-win-x64.ps1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$iscc = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if (-not $iscc) {
    $candidates = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC-x64.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 7\ISCC.exe"
    )
    $isccPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
} else {
    $isccPath = $iscc.Source
}

if (-not $isccPath) {
    throw "Inno Setup compiler was not found. Install Inno Setup 7 from https://jrsoftware.org/isinfo.php, then rerun scripts\build-installer.ps1."
}

New-Item -ItemType Directory -Force -Path .\dist | Out-Null
& $isccPath .\installer\quark.iss
