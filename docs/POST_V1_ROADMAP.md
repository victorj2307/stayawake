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
