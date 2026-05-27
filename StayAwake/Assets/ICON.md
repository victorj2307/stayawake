# Icon assets

See the [README](../../README.md) for build and publish instructions. Third-party attribution: [ATTRIBUTIONS.md](../../ATTRIBUTIONS.md).

| File | Purpose |
|------|---------|
| `app-icon-source.png` | Master artwork (edit this) |
| `app.ico` | Windows EXE, taskbar, tray |
| `app-header.png` | In-app header (transparent background) |

Regenerate `app.ico` and `app-header.png` after changing the source:

```powershell
python scripts/generate-icon.py
```

Requires [Pillow](https://pypi.org/project/pillow/): `pip install pillow`
