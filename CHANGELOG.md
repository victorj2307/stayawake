# Changelog

## v1.1.0

- Tray icon states: disabled, active, and session-completed variants
- Main window quick presets: 30m, 1h, 3h, indefinite (parity with tray menu)
- `sessionDurationMinutes` in settings for 30-minute preference persistence
- `develop` branch workflow documented ([docs/BRANCHING.md](docs/BRANCHING.md))
- EXE icon uses neutral/disabled appearance; tray uses state-specific ICOs
- Fix single-instance mutex release on exit (duplicate launch and shutdown)
- Fix preset chip layout alignment with the 140px settings value column

## v1.0.0

- Initial public release
