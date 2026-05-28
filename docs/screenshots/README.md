# StayAwake screenshots

Capture guide for README images. Use a **Release** build on Windows with dark theme enabled.

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
| `main-active.png` | Enable session, idle 60s, run duration 1h. Status: **Active**, remaining time visible. |
| `main-disabled.png` | Enabled off. All settings editable. |
| `main-session-completed.png` | Let a short session expire, or use `STAYAWAKE_SCREENSHOT=session-completed` (1s session). Status: **Session completed**; **Session ended** row shows local date and time. |
| `tray-menu.png` | Right-click tray icon with context menu fully visible. |

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

This captures the three main-window states. `tray-menu.png` is generated via:

```powershell
python StayAwake\scripts\generate-tray-menu-screenshot.py
```

The script uses `STAYAWAKE_SCREENSHOT=session-completed` for the completed-state capture (1-second session before window show).

## Example settings for active shot

```json
{
  "enabled": true,
  "movementPixels": 1,
  "idleSeconds": 60,
  "minimizeToTray": true,
  "movementMode": "Horizontal",
  "sessionDurationHours": 1
}
```

Place `settings.json` beside the EXE before launching for reproducible captures.
