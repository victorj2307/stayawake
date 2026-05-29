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

## Releasing (checklist)

Example for **v1.1.0**:

1. Finish and push work on `develop`.
2. Update [CHANGELOG.md](../CHANGELOG.md) on `develop` if needed; commit and push.
3. `git checkout main` → `git merge develop` → resolve conflicts if any.
4. `git push origin main`
5. From repo root on `main` with a **clean** tree: `.\scripts\release.ps1 -Version 1.1.0`
6. `git checkout develop` → `git merge main` (sync any release-only commits) → `git push origin develop`

Use GitHub’s private noreply address for `git config user.email` (e.g. `8624983+victorj2307@users.noreply.github.com`) so commits and annotated tags do not expose a personal email on the public repo.

Tags and GitHub Releases are created on **`main`** only (`v1.1.0`, etc.). `release.ps1` **errors** if you are not on `main` or if `main` is out of sync with `origin/main`.

## What we do not use

- GitFlow release branches
- Long-lived staging environments
- Tags on `develop`
