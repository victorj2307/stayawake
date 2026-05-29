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

$script:GhExe = $null

function Resolve-GhExe {
    $cmd = Get-Command gh -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    foreach ($path in @(
            "$env:ProgramFiles\GitHub CLI\gh.exe"
            ${env:ProgramFiles(x86)} + '\GitHub CLI\gh.exe'
            "$env:LOCALAPPDATA\Programs\GitHub CLI\gh.exe"
        )) {
        if ($path -and (Test-Path $path)) { return $path }
    }

    # Pick up PATH changes from a recent install without restarting the terminal.
    $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' +
        [Environment]::GetEnvironmentVariable('Path', 'User')
    $cmd = Get-Command gh -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    throw @"
GitHub CLI (gh) not found. Install from https://cli.github.com/ then either:
  - Open a new terminal and run: gh auth login
  - Or ensure gh.exe is on PATH
"@
}

function Invoke-Gh {
    param([string[]]$GhArgs)
    if (-not $script:GhExe) { $script:GhExe = Resolve-GhExe }
    & $script:GhExe @GhArgs
    if ($LASTEXITCODE -ne 0) {
        throw "gh $($GhArgs -join ' ') failed with exit code $LASTEXITCODE"
    }
}

function Get-ProjectVersion {
    [xml]$proj = Get-Content $csproj
    foreach ($pg in $proj.Project.PropertyGroup) {
        if ($null -ne $pg.Version) {
            return [string]$pg.Version
        }
    }
    throw "Could not find <Version> in $csproj"
}

function Set-ProjectVersion {
    param([string]$NewVersion)
    [xml]$proj = Get-Content $csproj
    $updated = $false
    foreach ($pg in $proj.Project.PropertyGroup) {
        if ($null -ne $pg.Version) {
            $pg.Version = $NewVersion
            $updated = $true
            break
        }
    }
    if (-not $updated) {
        throw "Could not find <Version> in $csproj"
    }
    $proj.Save((Resolve-Path $csproj).Path)
}

function Invoke-Git {
    param([string[]]$GitArgs)
    & git @GitArgs
    if ($LASTEXITCODE -ne 0) {
        throw "git $($GitArgs -join ' ') failed with exit code $LASTEXITCODE"
    }
}

function Get-GitHubRepoSlug {
    if (-not $script:GhExe) { $script:GhExe = Resolve-GhExe }
    $slug = & $script:GhExe repo view --json nameWithOwner -q .nameWithOwner 2>$null
    if ($LASTEXITCODE -eq 0 -and $slug) {
        return $slug.Trim()
    }

    $remote = git remote get-url origin 2>$null
    if ($remote -match 'github\.com[:/]([^/]+/[^/.]+)') {
        return $Matches[1]
    }

    throw 'Could not resolve GitHub repo slug. Set origin to github.com or run: gh auth login'
}

function Get-LatestReleaseTag {
    $tags = @(git tag --list 'v*' --sort=-v:refname)
    if ($tags.Count -gt 0) { return $tags[0] }
    return $null
}

function New-ReleaseNotesBody {
    param(
        [string]$Version,
        [string]$Tag,
        [string]$ZipName,
        [string]$PrevTag,
        [string]$RepoSlug
    )

    $templatePath = Join-Path $PSScriptRoot 'GITHUB_RELEASE_NOTES.md'
    if (-not (Test-Path $templatePath)) {
        throw "Release notes template not found: $templatePath"
    }

    $changelogLine = if ($PrevTag) {
        "**Full Changelog**: https://github.com/$RepoSlug/compare/${PrevTag}...${Tag}"
    } else {
        ''
    }

    $body = Get-Content -Path $templatePath -Raw -Encoding utf8
    $body = $body.Replace('{{TAG}}', $Tag)
    $body = $body.Replace('{{VERSION}}', $Version)
    $body = $body.Replace('{{ZIP_NAME}}', $ZipName)
    $body = $body.Replace('{{PREV_TAG}}', $(if ($PrevTag) { $PrevTag } else { '' }))
    $body = $body.Replace('{{REPO}}', $RepoSlug)
    $body = $body.Replace('{{CHANGELOG_LINE}}', $changelogLine)
    return ($body.TrimEnd() + "`n")
}

Push-Location $repoRoot
try {
    Assert-Command dotnet
    Assert-Command git
    if (-not $SkipPush) {
        $script:GhExe = Resolve-GhExe
        Write-Host "Using GitHub CLI: $script:GhExe"
        if (-not $DryRun) {
            & $script:GhExe auth status
            if ($LASTEXITCODE -ne 0) {
                throw 'gh is not authenticated. Run: gh auth login'
            }
        }
    }

    $status = git status --porcelain
    if ($status) {
        throw "Working tree is not clean. Commit or stash changes before releasing.`n$status"
    }

    $localTags = git tag -l $tag
    if ($localTags) {
        throw "Tag $tag already exists locally."
    }

    $remoteTag = git ls-remote --tags origin "refs/tags/$tag"
    if ($LASTEXITCODE -ne 0) {
        throw "git ls-remote failed. Check network and remote 'origin'."
    }
    if ($remoteTag) {
        throw "Tag $tag already exists on origin."
    }

    $branch = git rev-parse --abbrev-ref HEAD
    if ($branch -ne 'main') {
        Write-Warning "Current branch is '$branch', not 'main'."
    }

    $currentVersion = Get-ProjectVersion
    if ($currentVersion -eq $Version) {
        Write-Host "csproj already at version $Version; no version file change needed."
    } elseif (-not $DryRun) {
        Set-ProjectVersion -NewVersion $Version
        Write-Host "Updated csproj version: $currentVersion -> $Version"
    } else {
        Write-Host "[dry-run] Would set <Version> from $currentVersion to $Version in StayAwake.csproj"
    }

    $publishArgs = @(
        'publish', 'StayAwake\StayAwake.csproj',
        '-c', 'Release',
        '-r', 'win-x64',
        '--self-contained',
        '-p:PublishSingleFile=true',
        '-p:IncludeNativeLibrariesForSelfExtract=true'
    )

    if ($DryRun) {
        Write-Host "[dry-run] Publishing StayAwake (win-x64, single-file)..."
    } else {
        Write-Host 'Publishing StayAwake (win-x64, single-file)...'
        & dotnet @publishArgs
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed with exit code $LASTEXITCODE"
        }
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
        Compress-Archive -Path $exe -DestinationPath $zipPath -CompressionLevel Optimal -Force
        Write-Host "Created $zipPath"
    } else {
        Write-Host "[dry-run] Would zip $exe to $zipPath"
    }

    if ($SkipPush) {
        Write-Host 'SkipPush: done (no commit, tag, push, or GitHub release).'
        return
    }

    $previousTag = Get-LatestReleaseTag
    if ($previousTag) {
        Write-Host "Previous release tag: $previousTag (changelog compare baseline)"
    } else {
        Write-Host 'No prior v* tag; release notes will omit changelog compare link.'
    }

    Invoke-Step "Committing version $Version (if changed)..." {
        Invoke-Git @('add', $csproj)
        git diff --cached --quiet
        if ($LASTEXITCODE -eq 0) {
            Write-Host "No csproj changes to commit; tagging current HEAD."
        } else {
            Invoke-Git @('commit', '-m', "Release $tag")
        }
    }

    Invoke-Step "Creating tag $tag..." {
        Invoke-Git @('tag', '-a', $tag, '-m', "StayAwake $tag")
    }

    Invoke-Step 'Pushing branch and tag to origin...' {
        Invoke-Git @('push', 'origin', 'HEAD')
        Invoke-Git @('push', 'origin', $tag)
    }

    Invoke-Step "Creating GitHub release $tag..." {
        $notesFile = [System.IO.Path]::GetTempFileName()
        try {
            $repoSlug = Get-GitHubRepoSlug
            $notes = New-ReleaseNotesBody -Version $Version -Tag $tag -ZipName $zipName `
                -PrevTag $previousTag -RepoSlug $repoSlug
            Set-Content -Path $notesFile -Value $notes -Encoding utf8 -NoNewline
            Invoke-Gh @('release', 'create', $tag, $zipPath, '--title', "StayAwake $tag", '--notes-file', $notesFile)
        } finally {
            Remove-Item $notesFile -Force -ErrorAction SilentlyContinue
        }
    }

    Write-Host "Release $tag complete."
    if (-not $DryRun) {
        & $script:GhExe release view $tag --web 2>$null
    }
}
finally {
    Pop-Location
}
