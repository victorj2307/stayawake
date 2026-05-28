# StayAwake release: bump version, publish, zip, commit, tag, push, GitHub Release.
# Usage: .\scripts\release.ps1 -Version 1.0.1
#        .\scripts\release.ps1 -Version 1.0.1 -DryRun
#        .\scripts\release.ps1 -Version 1.0.1 -SkipPush

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [switch]$DryRun,
    [switch]$SkipPush
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$csproj = Join-Path $repoRoot 'StayAwake\StayAwake.csproj'
$publishDir = Join-Path $repoRoot 'StayAwake\bin\Release\net8.0-windows\win-x64\publish'
$exe = Join-Path $publishDir 'StayAwake.exe'
$distDir = Join-Path $repoRoot 'dist'
$zipName = "StayAwake-v$Version-win-x64.zip"
$zipPath = Join-Path $distDir $zipName
$tag = "v$Version"

function Invoke-Step {
    param([string]$Message, [scriptblock]$Action)
    if ($DryRun) {
        Write-Host "[dry-run] $Message"
        return
    }
    Write-Host $Message
    & $Action
}

function Assert-Command {
    param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found on PATH: $Name"
    }
}

Push-Location $repoRoot
try {
    Assert-Command dotnet
    Assert-Command git
    if (-not $SkipPush) {
        Assert-Command gh
        if (-not $DryRun) {
            gh auth status 2>&1 | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'gh is not authenticated. Run: gh auth login' }
        }
    }

    $status = git status --porcelain
    if ($status) {
        throw "Working tree is not clean. Commit or stash changes before releasing.`n$status"
    }

    if (git tag -l $tag) {
        throw "Tag $tag already exists locally."
    }

    $remoteTag = git ls-remote --tags origin "refs/tags/$tag"
    if ($remoteTag) {
        throw "Tag $tag already exists on origin."
    }

    $branch = git rev-parse --abbrev-ref HEAD
    if ($branch -ne 'main') {
        Write-Warning "Current branch is '$branch', not 'main'."
    }

    if (-not $DryRun) {
        [xml]$proj = Get-Content $csproj
        $updated = $false
        foreach ($pg in $proj.Project.PropertyGroup) {
            if ($null -ne $pg.Version) {
                $pg.Version = $Version
                $updated = $true
                break
            }
        }
        if (-not $updated) {
            throw "Could not find <Version> in $csproj"
        }
        $proj.Save((Resolve-Path $csproj).Path)
    } else {
        Write-Host "[dry-run] Would set <Version> to $Version in StayAwake.csproj"
    }

    $publishArgs = @(
        'publish', 'StayAwake\StayAwake.csproj',
        '-c', 'Release',
        '-r', 'win-x64',
        '--self-contained',
        '-p:PublishSingleFile=true',
        '-p:IncludeNativeLibrariesForSelfExtract=true'
    )

    Invoke-Step 'Publishing StayAwake (win-x64, single-file)...' {
        dotnet @publishArgs
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
    }

    if (-not $DryRun) {
        if (-not (Test-Path $exe)) {
            throw "Published executable not found: $exe"
        }

        $extra = Get-ChildItem $publishDir -Force | Where-Object { $_.Name -ne 'StayAwake.exe' }
        if ($extra) {
            $names = ($extra | ForEach-Object { $_.Name }) -join ', '
            Write-Warning "Publish folder contains extra files (expected only StayAwake.exe): $names"
        }

        New-Item -ItemType Directory -Path $distDir -Force | Out-Null
        if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
        Compress-Archive -Path $exe -DestinationPath $zipPath -Force
        Write-Host "Created $zipPath"
    } else {
        Write-Host "[dry-run] Would zip $exe to $zipPath"
    }

    if ($SkipPush) {
        Write-Host 'SkipPush: done (no commit, tag, push, or GitHub release).'
        return
    }

    Invoke-Step "Committing version $Version..." {
        git add $csproj
        git commit -m "Release $tag"
        if ($LASTEXITCODE -ne 0) { throw 'git commit failed' }
    }

    Invoke-Step "Creating tag $tag..." {
        git tag -a $tag -m "StayAwake $tag"
        if ($LASTEXITCODE -ne 0) { throw 'git tag failed' }
    }

    Invoke-Step 'Pushing branch and tag to origin...' {
        git push origin HEAD
        if ($LASTEXITCODE -ne 0) { throw 'git push failed' }
        git push origin $tag
        if ($LASTEXITCODE -ne 0) { throw 'git push tag failed' }
    }

    Invoke-Step "Creating GitHub release $tag..." {
        $notesFile = [System.IO.Path]::GetTempFileName()
        try {
            $hasPriorTag = [bool](git describe --tags --abbrev=0 HEAD^ 2>$null)
            if ($hasPriorTag) {
                gh release create $tag $zipPath --title "StayAwake $tag" --generate-notes
            } else {
                @"
StayAwake **$tag** — Windows x64 portable build.

## Install
1. Download ``$zipName`` from this release.
2. Extract and run ``StayAwake.exe`` (no installer; ``settings.json`` is created beside the EXE).

## Requirements
- Windows 10 or later
- No administrator rights required
"@ | Set-Content -Path $notesFile -Encoding utf8
                gh release create $tag $zipPath --title "StayAwake $tag" --notes-file $notesFile
            }
            if ($LASTEXITCODE -ne 0) { throw 'gh release create failed' }
        } finally {
            Remove-Item $notesFile -Force -ErrorAction SilentlyContinue
        }
    }

    Write-Host "Release $tag complete."
    if (-not $DryRun) {
        gh release view $tag --web 2>$null
    }
}
finally {
    Pop-Location
}
