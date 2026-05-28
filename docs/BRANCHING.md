# Branching

StayAwake uses a minimal two-branch workflow after v1.0.0.

## Branches

| Branch | Purpose |
|--------|---------|
| `main` | Stable, release-ready code. Annotated tags `vX.Y.Z` are created here only. |
| `develop` | Active iteration. Default target for day-to-day commits and pull requests. |

## Day-to-day

1. Branch from `develop` for features or fixes (e.g. `feature/tray-icon-states`).
2. Open pull requests into `develop`.
3. When ready to ship, merge `develop` → `main`.
4. On `main`, run [`scripts/release.ps1`](../scripts/release.ps1) with the new version (see [README § Releasing](../README.md#releasing)).
5. Merge `main` back into `develop` if the release script committed a version bump on `main` only.

## Hotfixes

1. Branch `hotfix/1.0.1-short-desc` from `main` at the release tag.
2. Fix, merge to `main`, release with `release.ps1`.
3. Merge or cherry-pick into `develop` so iteration stays in sync.

## Versioning

- **Patch** (`1.0.1`): hotfixes on `main`.
- **Minor** (`1.1.0`): post-v1 feature polish (tray icons, UI presets, etc.).
- Bump `<Version>` in `StayAwake/StayAwake.csproj` on `develop` before merging to `main`, or let `release.ps1` set it when releasing.

## What we do not use

- GitFlow release branches
- Long-lived staging environments
- Tags on `develop`
