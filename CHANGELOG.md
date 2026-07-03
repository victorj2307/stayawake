# Changelog

## v1.2.5

- Enabled toggle card shows a subtle green gradient background and green-tinted border while `Status` is **Active** (`EnabledCardBorder` style)
- Fix screenshot capture so `main-active.png` shows a real active session (`STAYAWAKE_SCREENSHOT=active`; ScreenshotTool writes camelCase `settings.json`)
- README screenshots refreshed for the active-state highlight

## v1.2.4

- Fix tray balloon notifications so a single click opens settings (session-completed and running-in-tray balloons)

## v1.2.3

- UI redesign: purple/navy theme with radial background glow, gradient header divider, and card elevation shadows
- Purple primary accent on toggles, focus rings, setting-row icons, and selected duration presets; green accent reserved for status (header, remaining time, active dot)
- Inset styling on numeric steppers, combo box, and text inputs for recessed field depth
- Reset settings button shows a purple refresh icon beside the label
- README screenshots refreshed for the updated theme

## v1.2.2

- New icons for settings rows (Segoe MDL2 Assets glyphs in the main window)
- Header separator line between title area and settings
- Relocate **Enabled** toggle to the right column above **Status**; settings-only left column
- Explicit Tab order for keyboard navigation (settings → Enabled → Reset settings)
- README screenshots refreshed for the updated layout

## v1.2.1

- Fix disabled **Movement direction** combo box styling to match other locked settings (disabled background, border, and muted text)
- Consistent vertical spacing between setting rows (top-aligned inputs; uniform row gaps; presets sit directly under the run-duration stepper)
- README screenshots refreshed for the updated settings layout

## v1.2.0

- Numeric setting ranges shown as muted hints (idle 10–3600 s, movement 1–10 px, run duration 0–99 h)
- Input guards on numeric fields: digits only, max length, clamp on blur; `SettingLimits` centralizes bounds
- Numeric steppers on idle, movement, and run duration (spinner buttons, Up/Down keys, and mouse wheel; idle steps by 10 s)
- Consistent keyboard focus ring (green border) on toggles, numeric steppers, combo box, preset chips, and reset button when tabbing
- `settings.json` values normalized on load (out-of-range values repaired and saved)
- Tray balloon when starting a session with **Minimize to tray** on, and when hiding the window to the tray (15 s cooldown between identical balloons)
- `SessionStarted` event distinguishes user-initiated starts from silent restore at startup (no balloon on restore)

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
