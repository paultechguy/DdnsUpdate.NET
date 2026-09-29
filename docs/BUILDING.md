# Building the single-file DdnsUpdate.exe

How to produce the one-file `DdnsUpdate.exe` that runs in production, and how to
deploy it.

## What this tree is

This line descends from `v0.1.0` (tag `e2252fd`, 2024-02-23), the version
production runs, and is now version `0.2.0` (see `CHANGELOG.md`). Settings files
sit **flat** beside the executable. There is no `config` subfolder anywhere in
this line, and the code has no concept of one: `Program.cs` sets the current
directory to the executable directory and reads `.\appsettings.*.json` from
there. Version 0.2.0 reads the same settings files as 0.1.0, so upgrading means
replacing only `DdnsUpdate.exe`.

Do not confuse this with the `master` line in the same GitHub repository. That
is an unrelated history with no common ancestor, it reads settings from a
`config` subfolder, and it names its executable `ddnsupdate.exe` in lowercase.
It also reports version `0.1.0`, so a `0.1.0` build cannot be told apart by its
version string. Use the settings location or the executable name casing instead.

## Prerequisites

.NET SDK 10.0 (pinned by `global.json`, which allows any later 10.0 feature
band). The libraries target `net10.0` and the executable targets
`net10.0-windows10.0.17763.0`. All packages resolve from nuget.org, with versions
managed centrally in `Directory.Packages.props`.

## Build

Open `DdnsUpdate.slnx` (repository root) in Visual Studio, or from the
repository root:

```powershell
dotnet build
dotnet test
dotnet publish src\DdnsUpdate.Application -p:PublishProfile=win-x64-single
```

The `win-x64-single` publish profile
(`src\DdnsUpdate.Application\Properties\PublishProfiles\win-x64-single.pubxml`)
sets Release, `win-x64`, self-contained, single file, and the `publish` output
folder at the repository root. It is equivalent to:

```powershell
dotnet publish src\DdnsUpdate.Application\DdnsUpdate.Application.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish
```

`DdnsUpdate.Application.csproj` sets `<AssemblyName>DdnsUpdate</AssemblyName>`,
so the published file is `DdnsUpdate.exe`.

Single file is required, not optional. `FilePathHelper` notes that
`Assembly.GetExecutingAssembly().Location` returns an empty string in a bundled
assembly, and works around it with `AppDomain.CurrentDomain.BaseDirectory`. That
workaround assumes a bundled publish.

Do not build from a deeply nested directory. Windows still limits paths to 260
characters by default, and project references fail past that with a misleading
`MSB9008` warning claiming the referenced project does not exist.

### Self-contained or framework-dependent

| Option | Command change | Result |
| --- | --- | --- |
| Self-contained | as written above | About 100 MB. Runs with no .NET runtime installed. |
| Framework-dependent | add `--self-contained false` (or edit the profile) | A few MB. Requires the .NET 10 runtime on the target machine. |

Check the size of the executable already in production to see which was used
last time. Either works.

## Publish output

```text
publish/
    DdnsUpdate.exe
    appsettings.json
    appsettings.production.json
    *.pdb
```

Flat, with no subfolders. The `.pdb` files are debug symbols and can be deleted
from a deployment.

`appsettings.production.user.json` is deliberately absent. It holds credentials
and the per-domain Cloudflare zone and record IDs, it is excluded by
`.gitignore`, and it lives only on the server. Deploying never overwrites it.

The `publish` folder is covered by `.gitignore` at any depth, so build output is
never committed.

## Deploying to a Windows server

1. **Stop the running updater first.** It runs on a timed loop, so a live
   process holds the executable and the copy will fail or leave a mixed set of
   files.
2. **Back up the whole application directory**, including the settings files.
3. Copy `DdnsUpdate.exe` over the existing one.
4. Leave `appsettings.production.user.json` in place. Do not move anything into
   a `config` folder; this version does not look there.
5. Restart the updater and confirm it reports the expected domain count.

## Verifying a deployment

A normal run proves very little. The updater only calls Cloudflare when the
detected public IP address differs from the stored one, so with an unchanged
address it skips every domain.

To force real API calls, temporarily set `alwaysUpdateDdnsEvenIfUnchanged` to
`true` under `applicationSettings.ddnsSettings`, run once, confirm every domain
reports success, then set it back to `false`.

The process exit code is 0 when a run finishes normally, including stopping
after `maximumDdnsUpdateIterations`, and 1 when startup fails (for example an
invalid `ipAddressProviders` URL) or the update loop throws. A Windows Service
that exits with 1 triggers its configured recovery actions.

For Cloudflare-proxied records, confirm afterwards that the orange cloud is
still on. This version sends a PATCH so that omitted fields such as `proxied`
are preserved. A PUT would reset them and silently disable the proxy, which is
the bug this build fixes.
