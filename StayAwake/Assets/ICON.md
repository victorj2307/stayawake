# Icon assets

See the [README](../../README.md) for build and publish instructions. Third-party attribution: [ATTRIBUTIONS.md](../../ATTRIBUTIONS.md).

| File | Purpose |
|------|---------|
| `app-icon-source.png` | Master artwork (edit this) |
| `app.ico` | Windows EXE icon (`ApplicationIcon`), embedded WPF resource (tray) |
| `app-header.png` | In-app header via pack URI (embedded WPF resource) |

Regenerate `app.ico` and `app-header.png` after changing the source:

```powershell
python scripts/generate-icon.py
```

Requires [Pillow](https://pypi.org/project/pillow/): `pip install pillow`
