# StayAwake screenshots

Capture guide for README images. Use a **Release** build on Windows with dark theme enabled.

Last full refresh: **v1.2.6** (status panel redesign: state headline, remaining-time progress bar, Activity card).

## Recommended specs

| Setting | Value |
|---------|--------|
| Window width | 680px (default) |
| Export scale | 1x or 2x (1360px wide for Retina) |
| Format | PNG |
| Crop | Window chrome only; optional 8–12px margin |

## Files

| File | How to capture |
|------|----------------|
| `main-active.png` | Enable session, idle 60s, run duration 1h. Status card headline **ACTIVE** (green) with the remaining-time progress bar and large centered countdown; green card highlight; Activity card shows the last-movement time. Captured via `STAYAWAKE_SCREENSHOT=active` (1h session started before window show). |
| `main-disabled.png` | Toggle off. All settings editable. Status card headline **INACTIVE** with "Windows can sleep normally" and the toggle upper-right (standard dark card, empty progress track); run-duration presets under the hours stepper; **Run in system tray** row with helper text. |
| `main-session-completed.png` | Let a short session expire, or use `STAYAWAKE_SCREENSHOT=session-completed` (1s session). Status card headline **COMPLETED**; the value shows the session-ended local date and time. Card back to standard appearance (toggle off). |
| `tray-menu.png` | Synthetic render of the tray menu during an **Active** session (`generate-tray-menu-screenshot.py`; not a live capture). |

## Capture steps

1. Build: `dotnet build StayAwake\StayAwake.csproj -c Release`
2. Run: `StayAwake\bin\Release\net8.0-windows\StayAwake.exe`
3. Set values per table above.
4. Capture with **Win+Shift+S** (window snip) or Snipping Tool.
5. Save PNGs to this folder using the exact filenames above.

## Automated capture

From the repo root (after Release build):

```powershell
.\StayAwake\scripts\capture-screenshots.ps1
```

This runs `ScreenshotTool` (main-window states) and `generate-tray-menu-screenshot.py` (`tray-menu.png`).

The script uses `STAYAWAKE_SCREENSHOT=active` for the active-state capture (1-hour session before window show) and `STAYAWAKE_SCREENSHOT=session-completed` for the completed-state capture (1-second session before window show).

## Manual capture (optional)

Prefer **`capture-screenshots.ps1`** for reproducible results. For hand captures:

### Active (`main-active.png`)

Place `settings.json` beside the EXE, then launch with the screenshot helper env var (starts a 1-hour session before the window appears):

```json
{
  "enabled": false,
  "movementPixels": 1,
  "idleSeconds": 60,
  "minimizeToTray": true,
  "movementMode": "Horizontal",
  "sessionDurationHours": 1
}
```

```powershell
$env:STAYAWAKE_SCREENSHOT = "active"
.\StayAwake\bin\Release\net8.0-windows\StayAwake.exe
```

### Inactive (`main-disabled.png`)

Use the same field values with `"enabled": false` and no env var; switch the toggle off if needed.

### Session completed (`main-session-completed.png`)

```powershell
$env:STAYAWAKE_SCREENSHOT = "session-completed"
.\StayAwake\bin\Release\net8.0-windows\StayAwake.exe
```

The app starts a 1-second session, completes it, then shows the window in **Session completed** state.
