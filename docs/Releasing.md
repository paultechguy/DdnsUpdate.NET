# Releasing DdnsUpdate

A release is two commands with your own judgement in between, and a click at the end.

```powershell
pwsh .\build\New-ReleaseNotes.ps1 -Version 0.3.0   # bump + scaffold the notes
#   ... write the notes, update CHANGELOG.md, commit them to develop ...
pwsh .\build\Publish-Release.ps1 -Version 0.3.0    # promote, build, tag, draft
#   ... smoke-test the draft, click Publish release ...
pwsh .\build\Publish-Release.ps1 -Version 0.3.0 -Verify
```

Nothing in that sequence is unrecoverable, which is deliberate and worth knowing before you
start. The rest of this document explains what each part does and why it is shaped that way.

The pipeline is a port of the one in Marqora. The shape and the safety properties are the
same; the branch names and the packaging are DdnsUpdate's own.

---

## Before the first release: join master to develop, once

Releases fast-forward `master` to `develop`, which only works when everything on `master` is
already on `develop`. Today it is not quite: `origin/master` carries its own copy of the
"Replace source with the v0.1.0 production line plus Cloudflare PATCH fix" commit (`c9468b6`),
made separately from `develop`'s (`b413b75`). The two commits contain **identical files**, but
git sees two histories, and the "master ancestor of develop" gate refuses to release until they
are joined.

Join them once, on `develop`, with an ordinary merge:

```powershell
git switch develop
git pull
git merge origin/master -m "Join master's history to develop"
git diff HEAD~1 --stat      # prints nothing: the merge changed no files
git push origin develop
```

After that `master` is an ancestor of `develop` and stays one, because `master` only ever
moves by the fast-forward in step 3. If the gate ever fails again, the fix is the same merge.

---

## What "ready to release" means

`Publish-Release.ps1` assumes `develop` is already the thing you want to ship. Specifically:

- The version in `Directory.Build.props` is the version you are releasing.
- `docs/releases/v<version>.md` exists, is written, and is committed.
- `CHANGELOG.md` has the same changes under that version.
- All of it is pushed, and your working tree is clean.

That is not a state some earlier script left behind — it is a plain fact about `develop`,
which you can check with `git log`. The release notes get there the same way every other
change does: you write them, you commit them, you push them.

**This is the main design decision in the whole pipeline.** A script that prepared the
release, paused for review, and then finished the job would put an irreversible push in the
middle of a script run and make "did that release start?" a question you could not answer by
looking at the repository. Making the notes an ordinary commit removes the question entirely.

---

## Step 1 — Scaffold the notes and bump the version

```powershell
pwsh .\build\New-ReleaseNotes.ps1 -Version 0.3.0
```

Changes exactly two things in your working tree and stops:

```
Directory.Build.props        <Version> set to 0.3.0
docs/releases/v0.3.0.md      scaffolded from build/release-notes-template.md
```

Nothing is committed. Nothing is pushed. If you change your mind, the script tells you the
two commands that undo it.

The gates here are the cheap ones — branch, tree, ancestry, and whether the version is still
free. Scaffolding a markdown file has no business running the test suite.

`-Check` runs those gates and stops, which answers "could I release from here?" without
starting anything. `-Force` overwrites notes you have already started.

## Step 2 — Write the notes and commit them

Fill in the placeholders. Delete any heading with nothing under it: an empty **Fixed**
section reads worse than no **Fixed** section. Always answer **Upgrading** — "nothing beyond
replacing `DdnsUpdate.exe`" is the most useful sentence an upgrader can read.

Write for someone deciding whether to put this on the server that keeps their DNS pointed at
home, not for someone reading the diff. The download link, the checksum, and the install and
upgrade steps are added automatically when the release is published, so do not repeat them.

Add the same changes to `CHANGELOG.md` under the new version, then:

```powershell
git add Directory.Build.props CHANGELOG.md docs/releases/v0.3.0.md
git commit -m "Release notes for 0.3.0"
git push origin develop
```

Take as long as you like. Nothing is waiting on you.

## Step 3 — Publish

```powershell
pwsh .\build\Publish-Release.ps1 -Version 0.3.0
```

Runs every gate, shows you exactly what it is about to do, and asks once. Then:

```
  [1/5] promote master     git merge --ff-only develop, then push
  [2/5] build from master  New-Release.ps1 -Test
  [3/5] tag                annotated v0.3.0, then push
  [4/5] release body       your notes plus the generated footer
  [5/5] draft              gh release create --draft, both assets attached
```

**This script writes no commits.** It promotes, builds, tags and uploads; that is all. It
returns you to `develop` on the way out, including when something fails.

`-WhatIf` runs all eight gates for real and then prints the git and `gh` commands without
running them. `-Yes` skips the confirmation for an unattended run.

## Step 4 — Smoke-test the draft, then publish it

The draft is private. Its asset is the literal file GitHub will serve, which is why the test
happens here rather than against a zip you built locally five minutes earlier:

```powershell
gh release download v0.3.0 --dir $env:TEMP\ddnsupdate-0.3.0
#   extract it, then in that folder:
.\DdnsUpdate.exe --version       # says 0.3.0
.\DdnsUpdate.exe --dry-run       # runs one pass, changes nothing
```

Happy with it? Click **Publish release** on the draft page. Not happy? Fix what is wrong on
`develop` and republish the same version — the next section.

## If the draft is wrong — republish the same version

A draft exists so that a version can fail. It is private, nobody has been told about it, and
its asset has been downloaded by you alone. So a version that fails its smoke test is built
again under the same number, rather than burning a version on a release nobody saw:

```powershell
#   ... fix it on develop, commit, push ...
pwsh .\build\Publish-Release.ps1 -Version 0.3.0 -Republish
```

`-Republish` adds one step ahead of the other five. It deletes the draft, then the tag on
origin, then the tag here, and releases 0.3.0 again from wherever `develop` now stands —
which means the fix you just pushed is in the artifact and in the commit `master` is tagged
at.

The confirmation asks you to type the version rather than `y`:

```
  About to replace v0.3.0

    delete    draft, tag on origin, local tag, for v0.3.0
    promote   master 1a2b3c4 -> 5d6e7f8 (2 commit(s), fast-forward)
    ...

  Type 0.3.0 to delete that draft and release it again:
```

The one thing it will not do is replace a release you have already published. A published
release has been announced to whoever watches the repository and its zip may already be on
somebody's server; changing what that version means after the fact is worse than the wrong
release. Once **Publish release** is clicked, the only way forward is the next version.

## Step 5 — Confirm what shipped

```powershell
pwsh .\build\Publish-Release.ps1 -Version 0.3.0 -Verify
```

Downloads both assets, checks the zip against its published checksum, and confirms the
archive's root holds exactly the six files it should: `DdnsUpdate.exe`, `appsettings.json`,
`appsettings.production.json`, `README.md`, `CHANGELOG.md` and `LICENSE.txt`.

---

## The gates

All eight run in `Publish-Release.ps1`; four of them (branch and tree, ancestry, tag, and a
notes check of their own) also run in `New-ReleaseNotes.ps1`. Every one is read-only, which is
what makes it safe to run them before deciding whether a release should happen at all.

| Gate | Refuses when |
|---|---|
| tooling | `gh` is missing or not signed in |
| branch and tree | You are not on `develop`, the tree is dirty, or `develop` and `origin/develop` disagree |
| `master` ancestor of `develop` | `master` has commits `develop` does not — the fast-forward would fail |
| tag and release | `v<version>` exists locally, on origin, or as a GitHub release and `-Republish` was not given; the release is already published, with or without `-Republish`; or the version does not come after the newest tag |
| version | `Directory.Build.props` disagrees with `-Version` |
| release notes | Missing, still carrying placeholders, or saying nothing the template did not |
| tests | `dotnet test` fails |
| build, no warnings | `dotnet build -warnaserror` fails |

Two are worth explaining.

**`master` ancestor of `develop`** is the one that would hurt most if it were missing. If
`master` has drifted, the fast-forward fails halfway through a release. The fix is never to
force-push `master` — it is to merge `master` into `develop` and release from the result, as
in the one-time step at the top of this document. Far better to learn that before the version
is bumped than halfway through a release.

**version** looks like a formality and is not. It catches the two mistakes people actually
make: releasing while the bump commit is still sitting unpushed, and typing last release's
number out of habit.

The tag gate accepts 0.2.0 after the existing `v0.1.0` tag; versions only have to move
forward.

---

## Why the build comes from `master`

`master` is where releases are tagged, so the shipped artifact is built there too. After the
fast-forward `master` and `develop` are the same commit, so the tree is identical either way
— building where you tag simply means the artifact and the tag cannot drift, even in
principle.

The `--ff-only` is a choice rather than a requirement. It is what makes `master` a plain
record of what was released: every commit on it is one that shipped, in the order it
shipped, and it never contains anything `develop` does not.

---

## The two templates

Release notes are assembled from two files, because **the checksum cannot exist before the
build that produces it**.

| File | Rendered | Ends up |
|---|---|---|
| `build/release-notes-template.md` | Step 1 | Committed at `docs/releases/v<version>.md` |
| `build/release-footer-template.md` | Step 3 | Appended to the release body; never committed |

Your notes are committed in step 2. The zip is not built until step 3. Committing a trial
build's hash would be wrong — .NET builds are not bit-reproducible, so a local rebuild hashes
differently — and committing a literal `{{SHA256}}` would leave a placeholder in the
repository forever. So the checksum, the download link and the install and upgrade boilerplate
live only in the footer, which is rendered at publish time and written to
`build/artifacts/release-body-<version>.md`.

`{{TOKEN}}` substitution is a plain string replace. `Expand-Template` refuses to return text
with tokens still in it, so a typo in a token name fails loudly rather than shipping
`{{SHA256}}` to the release page.

There is deliberately no `{{DATE}}`. Notes may be written days before the release, so a
scaffold-time date would be wrong by the time you publish, and the release page carries its
own date anyway.

---

## If something goes wrong

| Reached | How to undo |
|---|---|
| Step 1 finished | `git checkout -- Directory.Build.props`, delete the notes file |
| Step 2 committed and pushed | An ordinary commit. Revert it with another ordinary commit. |
| `master` fast-forwarded | Moves `master` to a commit already on `develop`. Harmless on its own. |
| Tag pushed | `git push --delete origin v0.3.0` then `git tag -d v0.3.0` |
| Draft created | `gh release delete v0.3.0 --yes` |
| Published | `gh release delete` removes the page, though watchers were already notified |

The tag and draft rows above are for abandoning a release. To redo one under the same number,
do not work through them by hand — `Publish-Release.ps1 -Version 0.3.0 -Republish` deletes the
draft and both copies of the tag itself, and then releases again.

---

## What is not automated

- **Building in CI.** The scripts are CI-shaped already — exit codes, `-Yes`, no prompts — so
  moving the publish into a GitHub Actions workflow later is a small step. The reason to do it
  is reproducible provenance, not ceremony: today the artifact's provenance is "the machine
  Paul ran this on".
- **Provenance attestation.** `gh attestation` is meaningful only for artifacts built in CI,
  so it follows the point above rather than leading it.
- **Code signing.** There is no certificate. The release footer explains the SmartScreen and
  downloaded-file consequences honestly rather than pretending them away.
