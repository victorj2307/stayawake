# Captures StayAwake window screenshots for docs/screenshots/
# Requires: Release build, Windows desktop session

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent | Split-Path -Parent
$exeDir = Join-Path $root "StayAwake\bin\Release\net8.0-windows"
$exe = Join-Path $exeDir "StayAwake.exe"
$outDir = Join-Path $root "docs\screenshots"

if (-not (Test-Path $exe)) {
    Write-Error "Build Release first: dotnet build StayAwake\StayAwake.csproj -c Release"
}

Add-Type -ReferencedAssemblies System.Drawing @"
using System;
using System.Drawing;
using System.Runtime.InteropServices;

public static class WindowCapture
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    public static Bitmap CaptureWindow(string title)
    {
        var hwnd = FindWindow(null, title);
        if (hwnd == IntPtr.Zero) return null;
        SetForegroundWindow(hwnd);
        RECT rect;
        if (!GetWindowRect(hwnd, out rect)) return null;
        int w = rect.Right - rect.Left;
        int h = rect.Bottom - rect.Top;
        if (w <= 0 || h <= 0) return null;
        var bmp = new Bitmap(w, h);
        using (var g = Graphics.FromImage(bmp))
        {
            var hdc = g.GetHdc();
            try { PrintWindow(hwnd, hdc, 2); }
            finally { g.ReleaseHdc(hdc); }
        }
        return bmp;
    }
}
"@

function Write-Settings($path, $json) {
    $json | Set-Content -Path $path -Encoding UTF8
}

function Capture-State($name, $settingsJson) {
    Get-Process StayAwake -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500

    $settingsPath = Join-Path $exeDir "settings.json"
    Write-Settings $settingsPath $settingsJson

    $proc = Start-Process -FilePath $exe -WorkingDirectory $exeDir -PassThru
    Start-Sleep -Seconds 2

    $bmp = [WindowCapture]::CaptureWindow("StayAwake")
    if ($null -eq $bmp) {
        Write-Warning "Could not capture window for $name"
        $proc | Stop-Process -Force -ErrorAction SilentlyContinue
        return
    }

    $outPath = Join-Path $outDir "$name.png"
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Saved $outPath"

    $proc | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

Capture-State "main-disabled" @'
{
  "enabled": false,
  "movementPixels": 1,
  "idleSeconds": 60,
  "minimizeToTray": true,
  "movementMode": "Horizontal",
  "sessionDurationHours": 0
}
'@

Capture-State "main-active" @'
{
  "enabled": true,
  "movementPixels": 1,
  "idleSeconds": 60,
  "minimizeToTray": true,
  "movementMode": "Horizontal",
  "sessionDurationHours": 1
}
'@

function Capture-SessionCompleted {
    Get-Process StayAwake -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500

    $settingsPath = Join-Path $exeDir "settings.json"
    Write-Settings $settingsPath @'
{
  "enabled": false,
  "movementPixels": 1,
  "idleSeconds": 60,
  "minimizeToTray": true,
  "movementMode": "Horizontal",
  "sessionDurationHours": 0
}
'@

    $env:STAYAWAKE_SCREENSHOT = "session-completed"
    $proc = Start-Process -FilePath $exe -WorkingDirectory $exeDir -PassThru
    Start-Sleep -Seconds 3

    $bmp = [WindowCapture]::CaptureWindow("StayAwake")
    if ($null -ne $bmp) {
        $bmp.Save((Join-Path $outDir "main-session-completed.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        Write-Host "Saved main-session-completed.png"
    }

    Remove-Item Env:STAYAWAKE_SCREENSHOT -ErrorAction SilentlyContinue
    $proc | Stop-Process -Force -ErrorAction SilentlyContinue
}

Capture-SessionCompleted

Write-Host "Capture tray-menu.png manually, or run StayAwake and snip the tray context menu."
