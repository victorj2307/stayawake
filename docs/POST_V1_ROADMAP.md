# StayAwake post-v1.0.0 roadmap

Summary of the first iteration after the stable v1.0.0 release. See [BRANCHING.md](BRANCHING.md) for git workflow.

## Shipped in v1.1.0

| Initiative | Summary |
|------------|---------|
| **Branching** | `main` = releases; `develop` = iteration |
| **Tray icon states** | `app-tray-disabled/active/completed.ico`; `UpdateTrayAppearance` on `StatusChanged` |
| **UI presets** | Compact chips under run duration; same durations as tray |
| **30m settings sync** | `SessionDurationMinutes` in `settings.json` |
| **Preset chip semantics** | Highlight = saved preference only; runtime state = tray, status card, **Enabled** |
| **Post-ship fixes (in v1.1.0)** | Mutex release on exit; preset chips aligned to 140px value column |

## Shipped in v1.2.0

| Initiative | Summary |
|------------|---------|
| **Setting bounds** | `SettingLimits`, muted range hints, digits/max-length guards, normalize on load, `NumericStepper` (buttons, Up/Down, mouse wheel), Tab focus styling |
| **Tray-running balloon** | Notify when session starts with minimize-to-tray or window hides to tray (15 s cooldown) |

## Shipped in v1.2.1

| Initiative | Summary |
|------------|---------|
| **Disabled combo styling** | `UtilityComboBox` disabled state matches other locked inputs |
| **Setting-row layout** | Top-aligned controls; uniform 10px row gaps; run-duration presets directly under stepper |
| **Docs** | README screenshots regenerated for current UI |

## Shipped in v1.2.2

| Initiative | Summary |
|------------|---------|
| **Setting icons** | Updated Segoe MDL2 Assets glyphs for each settings row |
| **Layout** | Enabled toggle on right column above Status; settings-only left column; header divider |
| **Tab order** | Explicit `TabIndex` 1–11 for keyboard navigation |
| **Docs** | README screenshots regenerated for updated layout |

## Shipped in v1.2.3

| Initiative | Summary |
|------------|---------|
| **UI theme** | Purple/navy redesign: radial background glow, gradient divider, card elevation shadows, inset inputs |
| **Accents** | Purple primary on toggles, focus, icons, presets; green reserved for status section |
| **Docs** | README screenshots and architecture docs updated for the new theme |

## Shipped in v1.2.4

| Initiative | Summary |
|------------|---------|
| **Tray balloons** | Single click on session-completed or running-in-tray balloon opens settings |

## Shipped in v1.2.5

| Initiative | Summary |
|------------|---------|
| **Enabled card highlight** | Green gradient on Enabled toggle card when `AppStatus.Active`; `EnabledCardBorder` in `App.xaml` |
| **Screenshot capture** | `STAYAWAKE_SCREENSHOT=active` for active-state PNG; ScreenshotTool camelCase `settings.json` |
| **Docs** | Architecture, changelog, and README screenshots updated for v1.2.5 |

## Shipped in v1.2.6

| Initiative | Summary |
|------------|---------|
| **Status panel redesign** | Prominent status card: `ACTIVE`/`INACTIVE` headline + state icon, description, upper-right toggle, large centered remaining time + caption; separate **Activity** card for last movement |
| **Remaining-time progress bar** | Subtle green `StatusProgressBar` bound to `MainViewModel.RemainingFraction` (decreases when timed, full when unlimited, empty otherwise); always occupies constant height; synced to the per-second countdown |
| **Last movement** | `LastMovementValue` shows the clock time (`HH:mm:ss`) of the last synthetic movement (`Never` before the first jiggle) |
| **Green reserved for Active** | Green now used only in the active state (headline, remaining, progress, state icon, card highlight) |
| **Label** | **Minimize to tray** renamed to **Run in system tray** with helper text (setting unchanged) |
| **Docs** | Architecture, changelog, screenshots README, and README screenshots updated for v1.2.6 |

## v1.2+ (optional, not blocking)

| Initiative | Summary |
|------------|---------|
| **Custom branding** | Replace Flaticon `app-icon-source.png` with owned vector art; refresh README screenshots |
| **CHANGELOG** | Keep [CHANGELOG.md](../CHANGELOG.md) updated per release |

Regenerate icons after art changes: `StayAwake/scripts/generate-icon.py`.

## Product stewardship

Before adding features, confirm: tray-first, session-oriented, maintainable by one developer, no new moving parts (timers, network, plugins).

**Reject by default:** cloud sync, accounts, telemetry, schedulers, dashboards, enterprise deployment tooling.

**Accept when aligned:** tray/UI polish, Win32 fixes, small settings that stay off by default.
