# StayAwake

A minimal Windows utility that keeps your PC awake when you need it—by detecting idle time and nudging the mouse in place, without moving the cursor from where you left it.

---

## Philosophy

StayAwake is intentionally **small**, **portable**, and **easy to reason about**:

- **One executable** — copy `StayAwake.exe` anywhere; settings live beside it as JSON.
- **No installer, no registry** — nothing to uninstall; delete the folder when done.
- **Tray-first** — runs quietly in the background; the window is for configuration.
- **Session-based** — enable for a set time or indefinitely; auto-stops when the session ends.
- **Minimal UI** — no dashboard, no accounts, no telemetry—just the controls you need.

The architecture stays flat on purpose: manual wiring in `App`, one background worker. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full technical reference.

---

## Features

- **Idle detection** via Windows `GetLastInputInfo`
- **Invisible mouse jiggle** — `SendInput` moves the cursor slightly and returns it (configurable pixels and direction)
- **Keep-awake** — `SetThreadExecutionState` prevents display sleep and system idle while a session is active
- **Configurable idle threshold** — how long to wait after your last real input before nudging (10–3600 seconds)
- **Session duration** — run for N hours or unlimited (`0` hours); auto-disable when time is up
- **System tray** — quick-start presets (30 min, 1 h, 3 h, indefinite), stop session, open settings
- **Portable settings** — `settings.json` next to the executable
- **Dark, compact UI** — single settings screen with live status (including session end date/time after a timed session completes)
- **Single instance** — prevents accidentally running two copies

---

## Screenshots

| Active session | Disabled |
|----------------|----------|
| ![Main window — active session](docs/screenshots/main-active.png) | ![Main window — disabled](docs/screenshots/main-disabled.png) |

| Session completed (shows **Session ended** time) | Tray menu |
|-------------------|-----------|
| ![Session completed](docs/screenshots/main-session-completed.png) | ![Tray context menu](docs/screenshots/tray-menu.png) |

To regenerate images, see [docs/screenshots/README.md](docs/screenshots/README.md).

---

## Requirements

- **Windows 10** or later
- **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** (to build from source)

No administrator rights required to run.

---

## Quick start

1. Build or download `StayAwake.exe` (see [Publish](#publish-single-portable-exe) below).
2. Run it. On first launch, `settings.json` is created beside the EXE.
3. Turn **Enabled** on and set **Idle time** (e.g. `60` seconds).
4. Leave the PC idle — after the threshold, the mouse nudges once per idle period (cursor stays in place).
5. Optional: enable **Minimize to tray** and close the window; the app keeps running from the tray icon.

---

## Build

```powershell
cd path\to\stayawake
dotnet build StayAwake\StayAwake.csproj -c Release
```

Output: `StayAwake\bin\Release\net8.0-windows\StayAwake.exe`

---

## Publish (single portable EXE)

```powershell
dotnet publish StayAwake\StayAwake.csproj -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Output:

```
StayAwake\bin\Release\net8.0-windows\win-x64\publish\StayAwake.exe
```

Copy `StayAwake.exe` anywhere. `settings.json` is created on first run in the same directory.

### Releasing

Automated release to GitHub via [`scripts/release.ps1`](scripts/release.ps1): publish a single-file EXE, zip it, commit the version bump (if needed), tag, push, and create a GitHub Release.

#### One-time setup

1. Install [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and [GitHub CLI](https://cli.github.com/).
2. Log in to GitHub (required before a full release):

   ```powershell
   gh auth login
   gh auth status
   ```

3. After installing `gh`, open a **new** terminal so `gh` is on `PATH`. The release script also looks for `C:\Program Files\GitHub CLI\gh.exe` if needed.

#### Workflow

1. Commit and push all changes on `main` (the script requires a **clean** working tree).
2. Choose `-Version` to match the release tag (`1.0.1` → tag `v1.0.1`). If `StayAwake.csproj` already has that version, the script skips a version commit and tags the current `HEAD`.
3. Test the build locally, then run a full release:

   ```powershell
   .\scripts\release.ps1 -Version 1.0.1 -SkipPush   # build + zip only
   .\scripts\release.ps1 -Version 1.0.1            # tag, push, GitHub Release
   ```

| Switch | Purpose |
|--------|---------|
| `-DryRun` | Print steps without publish, commit, tag, push, or `gh release` |
| `-SkipPush` | Publish and zip to `dist/` only (no git or GitHub steps) |

**Prerequisites for a full release:** Windows, .NET 8 SDK, authenticated `gh`, clean working tree on `main`, push access to `origin`.

**Release asset:** `dist/StayAwake-v{version}-win-x64.zip` containing **only** `StayAwake.exe` (icons embedded in the assembly). The `dist/` folder is gitignored.

#### Troubleshooting

| Problem | What to do |
|---------|------------|
| Working tree is not clean | Commit or stash changes, then rerun |
| `gh` not found | Install GitHub CLI and open a new terminal, or verify `C:\Program Files\GitHub CLI\gh.exe` exists |
| `gh is not authenticated` | Run `gh auth login` |
| Tag already exists | Use a new `-Version`, or delete the tag on GitHub if the release was a mistake |
| Push succeeded but `gh release create` failed | Create the release manually: `gh release create v1.0.1 dist/StayAwake-v1.0.1-win-x64.zip --title "StayAwake v1.0.1"` |

See [docs/ARCHITECTURE.md §16](docs/ARCHITECTURE.md#16-release-automation) for a short technical summary.

---

## Usage

### Enable a session (main window)

1. Set **Run duration (hours)** — `0` = unlimited until you stop the session.
2. Configure **Idle time**, **Movement**, and **Movement direction** as needed.
3. Toggle **Enabled** on. Settings lock while active; turn off to edit again.

### Tray menu

Right-click the tray icon:

| Menu item | Action |
|-----------|--------|
| Open settings | Show the main window |
| Start 30 minutes / 1 hour / 3 hours | Start a timed session |
| Start indefinitely | Start with no time limit |
| Stop session | End the current session |
| Exit | Quit the application |

Double-click the tray icon to open settings.

When a timed session ends, a tray balloon notifies you and status shows **Session completed**.

### Minimize to tray

With **Minimize to tray** enabled, closing or minimizing the window hides it; the worker and tray icon keep running until you choose **Exit** from the tray.

---

## Architecture overview

```
App (composition root)
 ├── SettingsStore → AppSettings (JSON)
 ├── StayAwakeWorker (1s timer loop) → NativeMethods (Win32)
 ├── MainViewModel → MainWindow (WPF)
 └── TrayIconManager (WinForms NotifyIcon)
```

**Runtime states:** `Disabled` → `Active` (session running) → `SessionCompleted` (timed session ended).

For state diagrams, worker loop pseudocode, Win32 details, and threading notes, see **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)**.

---

## Runtime / session model

| State | Meaning |
|-------|---------|
| **Disabled** | Not keeping the system awake; settings editable |
| **Active** | Session running; keep-awake on; settings locked |
| **Session completed** | Timed session ended automatically; settings editable |

**Lifecycle:**

- **Start** — `StartSession(duration)` sets timestamps, enables keep-awake, begins idle monitoring.
- **Stop** — `StopSession()` or tray **Stop session** ends the session.
- **Auto-stop** — when `SessionEndsAt` is reached, status becomes **Session completed**, settings saved, tray balloon shown; the Status panel shows **Session ended** with the local date and time (in memory until you start a new session or disable).

**Idle behavior:** Each second, if you've been idle for at least `IdleSeconds` *and* the last synthetic jiggle was at least `IdleSeconds` ago, the worker nudges the mouse and resets the rate limit.

---

## UI philosophy

- **Compact utility** — one screen, no tabs or dashboards.
- **Dark theme** — low-contrast grays and green accent; Segoe MDL2 icons for settings rows.
- **Status sidebar** — state, remaining time (or session ended date/time when completed), last movement—no log viewer.
- **Session-oriented workflow** — enable = start a session; configure duration and idle rules before or between sessions.

---

## Technical summary

| Mechanism | API / approach |
|-----------|----------------|
| Idle detection | `user32!GetLastInputInfo` |
| Mouse nudge | `user32!SendInput` (relative move out and back) |
| Prevent sleep | `kernel32!SetThreadExecutionState` while Active |
| Background loop | `PeriodicTimer` at 1 second |
| Settings | `System.Text.Json` → `settings.json` beside EXE |
| Movement mode | `MovementMode` enum with tolerant JSON converter |
| UI | WPF + minimal MVVM (`MainViewModel`) |
| Tray | Windows Forms `NotifyIcon` |

---

## Project structure

```
stayawake/
├── LICENSE
├── ATTRIBUTIONS.md
├── README.md
├── scripts/
│   └── release.ps1
├── docs/
│   ├── ARCHITECTURE.md
│   └── screenshots/
├── StayAwake.slnx
└── StayAwake/
    ├── App.xaml / App.xaml.cs
    ├── MainWindow.xaml / MainWindow.xaml.cs
    ├── MainViewModel.cs
    ├── StayAwakeWorker.cs
    ├── TrayIconManager.cs
    ├── NativeMethods.cs
    ├── AppSettings.cs
    ├── AppStatus.cs
    ├── MovementMode.cs
    ├── MovementModeJsonConverter.cs
    ├── SessionDisplay.cs
    ├── SettingsStore.cs
    ├── RelayCommand.cs
    ├── StayAwake.csproj
    ├── Assets/
    └── scripts/
        ├── generate-icon.py
        ├── capture-screenshots.ps1
        └── generate-tray-menu-screenshot.py
```

---

## Settings (`settings.json`)

Created beside `StayAwake.exe` on first run.

```json
{
  "enabled": false,
  "movementPixels": 1,
  "idleSeconds": 60,
  "minimizeToTray": false,
  "movementMode": "Horizontal",
  "sessionDurationHours": 0
}
```

| Field | Type | Description |
|-------|------|-------------|
| `enabled` | bool | Whether a session is active (persisted across restarts) |
| `movementPixels` | int | Jiggle distance (1–10) |
| `idleSeconds` | int | Seconds of real idle before nudging (10–3600) |
| `minimizeToTray` | bool | Hide window on close/minimize instead of exiting |
| `movementMode` | enum | `Horizontal`, `Vertical`, or `Random` (unknown values default to `Horizontal`) |
| `sessionDurationHours` | int | Default duration when enabling from UI (`0` = unlimited) |

Tray presets start sessions directly and do not change `sessionDurationHours` in the file.

---

## Tray behavior

- **Icon:** Embedded `app.ico` WPF resource (fallback: system default).
- **Tooltip:** `StayAwake — Active`, `Active (1h 12m)`, `Active (no limit)`, `Disabled`, or `Session completed` (63-char limit).
- **Menu:** Rebuilt when opened; start presets disabled while a session is active.
- **Balloon:** "Session completed" when a timed session expires.

---

## Technical constraints

- **Windows only** — relies on Win32 APIs not available on other platforms.
- **Synthetic input** — jiggles count as input for Windows idle/lock; may not satisfy all third-party "presence" tools.
- **Policy overrides** — corporate Group Policy can still enforce lock or sleep.
- **No installer** — user manages the EXE and `settings.json` manually.

---

## Known limitations

- Tray **30 minutes** preset does not update the **Run duration (hours)** field (1h/3h/indefinite presets sync where applicable). Remaining time in the status panel always reflects the active session.

Platform and policy constraints: [docs/ARCHITECTURE.md §13](docs/ARCHITECTURE.md#13-weaknesses-and-risks).

---

## Roadmap / future ideas

- Tray icon visual state (active / disabled / completed)
- UI session presets matching tray (30m / 1h / 3h)

**Not planned:** cloud sync, accounts, telemetry, plugins, schedulers, dashboards.

Full list: [docs/ARCHITECTURE.md § Future opportunities](docs/ARCHITECTURE.md#14-future-opportunities).

---

## Contributing

StayAwake is meant to stay **small and understandable**:

- Match existing style: flat structure, no unnecessary abstractions.
- Avoid DI frameworks, plugin systems, or extra architecture layers.
- Prefer focused changes over large refactors.
- Update `docs/ARCHITECTURE.md` when behavior or structure changes meaningfully.

### Icon assets

See [StayAwake/Assets/ICON.md](StayAwake/Assets/ICON.md) for regenerating `app.ico` and `app-header.png`. Third-party icon credits: [ATTRIBUTIONS.md](ATTRIBUTIONS.md).

### Screenshots

See [docs/screenshots/README.md](docs/screenshots/README.md) and `StayAwake/scripts/capture-screenshots.ps1`.

---

## Third-party assets

Icon and asset credits: [ATTRIBUTIONS.md](ATTRIBUTIONS.md).

---

## License

[MIT](LICENSE) — see [LICENSE](LICENSE) for details.

---

## Related documentation

- **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)** — Technical reference: runtime model, worker loop, Win32, threading, health review.
