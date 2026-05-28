# Icon assets

See the [README](../../README.md) for build and publish instructions. Third-party attribution: [ATTRIBUTIONS.md](../../ATTRIBUTIONS.md).

| File | Purpose |
|------|---------|
| `app-icon-source.png` | Master artwork (edit this) |
| `app.ico` | EXE icon (`ApplicationIcon`), neutral/disabled appearance |
| `app-tray-disabled.ico` | Tray when disabled |
| `app-tray-active.ico` | Tray when session active |
| `app-tray-completed.ico` | Tray when timed session completed |
| `app-header.png` | In-app header via pack URI (embedded WPF resource) |

Regenerate all icons after changing the source:

```powershell
cd StayAwake
python scripts/generate-icon.py
```

Requires [Pillow](https://pypi.org/project/pillow/): `pip install pillow`
