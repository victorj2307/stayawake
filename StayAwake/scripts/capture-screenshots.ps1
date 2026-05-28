# Captures StayAwake window screenshots for docs/screenshots/
# Requires: Release build, Windows desktop session

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent | Split-Path -Parent
$exe = Join-Path $root 'StayAwake\bin\Release\net8.0-windows\StayAwake.exe'
$toolProject = Join-Path $PSScriptRoot 'ScreenshotTool\ScreenshotTool.csproj'

if (-not (Test-Path $exe)) {
    Write-Error "Build Release first: dotnet build StayAwake\StayAwake.csproj -c Release"
}

dotnet run --project $toolProject -c Release -- $root
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Regenerating tray-menu.png...'
python (Join-Path $PSScriptRoot 'generate-tray-menu-screenshot.py')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Done.'
