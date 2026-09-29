# DdnsUpdate build scripts

Everything here is PowerShell 7 (`pwsh`) and everything is run from the repository root:

```powershell
pwsh .\build\<script>.ps1
```

All of them exit `0` on success and `1` on failure, so any one of them can gate a commit or a
CI step. The one exception to "run from the root" is `ReleaseCommon.ps1`, which is a library and
is dot-sourced by the others rather than invoked.

The full release walkthrough — why it is shaped the way it is, and how to undo each step — is
[docs/Releasing.md](../docs/Releasing.md).

---

## Building something deployable

| Script | What it does |
|---|---|
| `New-Release.ps1` | Publishes with the `win-x64-single` profile and packs `build\artifacts\DdnsUpdate-<version>-win-x64.zip` plus a `.sha256`, taking the version from the built executable. The zip is flat: `DdnsUpdate.exe`, `appsettings.json`, `appsettings.production.json`, `README.md`, `CHANGELOG.md`, `LICENSE.txt`. `-Test` gates it on a green test run; `-KeepStaging` leaves the staged folder for inspection. |

For a quick local build without a zip, `dotnet publish src\DdnsUpdate.Application -p:PublishProfile=win-x64-single`
writes to `publish\` at the repository root.

## Releasing

Two steps with your own work in between. The split is deliberate: nothing reaches GitHub until
the notes are written and committed.

| Script | What it does |
|---|---|
| `New-ReleaseNotes.ps1` | **Step 1.** Bumps `<Version>` in `Directory.Build.props` and scaffolds `docs\releases\v<version>.md` from the template. You then write the notes, update `CHANGELOG.md`, and commit them as an ordinary change on `develop`. `-Check` asks whether a release could start, without starting one. |
| `Publish-Release.ps1` | **Step 2.** Fast-forwards `master` to `develop`, builds via `New-Release.ps1 -Test`, tags, and leaves a **draft** release on GitHub. Nothing it does is unrecoverable. `-WhatIf` runs every gate for real and then prints the git and gh commands without running them; `-Republish` replaces a draft; `-Verify` re-checks a release you have already published. |

## Shared

| Script | What it does |
|---|---|
| `ReleaseCommon.ps1` | Not run directly. Dot-sourced by the three scripts above for the progress output, the `dotnet`/`git`/`gh` wrappers, the version helpers, the pre-flight gates, and the staging and zip steps. Defines functions and does nothing on its own. |

`release-notes-template.md` and `release-footer-template.md` sit beside the scripts. Both use
`{{TOKEN}}` substitution.

## What touches what

| Script | `build\artifacts\` | Outside `build\` |
|---|---|---|
| `New-Release.ps1` | **writes** the zip and `.zip.sha256`; `.stage\` while it runs | — |
| `New-ReleaseNotes.ps1` | — | writes `docs\releases\v<version>.md`; edits `Directory.Build.props` |
| `Publish-Release.ps1` | **writes** `release-body-<version>.md`; reads what `New-Release.ps1` left | moves `master`, writes a tag, uploads a draft to GitHub. `-Verify` downloads to `%TEMP%` |
| `ReleaseCommon.ps1` | writes, for its callers | — |

`build\artifacts\` is git-ignored.
