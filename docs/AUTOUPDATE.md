# Auto-update (Option A — GitHub Releases as the update server)

The Council checks its own [GitHub Releases](https://github.com/ilyafainberg/TheCouncil/releases)
for a newer version, downloads the right asset, and swaps itself in place. No
backend, no third-party packages — just the public GitHub API.

| File | Where | Role |
| --- | --- | --- |
| `Updating/UpdateChecker.cs` | source | checks the API, compares versions, downloads, launches the helper |
| `apply-update.cmd` | next to the built `.exe` (copied on build) | waits for the app to exit, applies the update, relaunches |

## Flow

```
App startup (MainForm.Shown)
        │
        ▼
GET api.github.com/repos/ilyafainberg/TheCouncil/releases/latest
        │   compare tag (v1.3.0 -> 1.3.0) vs this build's Version
        ▼
   newer? ──no──► do nothing (silent)
        │yes
        ▼
ask the user → pick asset by install kind:
   portable  -> TheCouncil-1.3.0-portable-win-x64.zip
   installer -> TheCouncil-1.3.0-setup.zip
        │
        ▼
download to %TEMP%, launch apply-update.cmd <pid> <kind> <zip> <dir> <exe>
        │
   app exits ──► helper waits for PID to die
        │
        ├─ portable : Expand-Archive + robocopy over the install folder, relaunch
        └─ installer: extract setup.exe, run /VERYSILENT (UAC), it relaunches
```

Because the build is **self-contained**, the portable zip carries the .NET runtime,
so a file-swap update never breaks on a machine without .NET.

## How it's wired in

- **Startup check** — `MainForm` subscribes to `Shown` and calls
  `CheckForUpdatesAsync(silentIfNone: true)`. It never blocks the UI and swallows
  network errors, so a flaky connection is invisible. If a newer **stable** release
  exists, the user is prompted with the release notes and asked to update.
- **Version source** — the updater reads `AssemblyInformationalVersion`. CI passes
  `-p:Version=<tag>` (see `release.yml`), so released builds know their own version.
  Local dev builds report `0.0.0`, so they always think an update is available — that
  only matters in development.
- **Install-kind detection** — the Inno installer writes an `installed.marker` file
  next to the app (`[Code]` section in `installer/setup.iss`); its presence means the
  *-setup.zip is fetched. Portable builds have no marker and fetch the portable zip.

## Edge cases handled

- **Network down / GitHub 403 / timeout** → treated as "no update", never crashes.
- **Prereleases / drafts** → skipped; only stable releases auto-update.
- **`1.10.0` vs `1.9.0`** → compared as `System.Version`, not strings.
- **Locked exe** → solved by the separate helper process + PID wait.
- **Failed extract** → helper aborts and leaves a message; the installer path is
  atomic (Inno handles rollback).

## Deliberately NOT done

- **Delta updates** — it re-downloads the whole zip. Fine for an app this size.
- **Checksum verification of the download** — GitHub HTTPS plus the published
  `SHA256SUMS.txt` cover integrity; a hash check could be added in
  `DownloadAndApplyAsync` for belt-and-braces.
