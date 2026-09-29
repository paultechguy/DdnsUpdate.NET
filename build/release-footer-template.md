---

## Download

**[{{ZIP}}]({{DOWNLOAD_URL}})** — Windows 10 1809 (build 17763) or later, 64-bit.

Self-contained: the .NET runtime is inside `DdnsUpdate.exe`, so the server needs nothing
installed first. The zip holds the executable, its two settings templates
(`appsettings.json`, `appsettings.production.json`), and `README.md`, `CHANGELOG.md` and
`LICENSE.txt`, all at its root.

## New installation

Extract the zip into a folder of its own, such as `C:\Apps\DdnsUpdate`, then follow the
[README]({{README_URL}}): configure your domains, try a dry run, and install it as a Windows
Service or a scheduled task.

## Upgrading

1. Stop the updater — the service, or the scheduled task if one is running.
2. Back up the application folder, settings files included.
3. Copy **only `DdnsUpdate.exe`** from the zip over the old one.
4. Start the updater again and check the log for the expected domain count.

Your `appsettings.production.user.json` — credentials, zone and record IDs — is untouched,
because the zip never contains one. Do not extract the whole zip over an existing
installation: that would replace `appsettings.production.json`, which you may have edited.

## About the security warnings

DdnsUpdate is not code-signed — a certificate costs several hundred dollars a year, which is
hard to justify for a free tool. Windows marks files downloaded from the internet, and may
warn the first time the executable runs. Clear the mark once, after extracting:

```powershell
Unblock-File .\DdnsUpdate.exe
```

If you start it from Explorer instead and see the blue **Windows protected your PC** panel,
click **More info**, then **Run anyway**.

## Verify your download

```
SHA256  {{SHA256}}
```

```powershell
Get-FileHash {{ZIP}} -Algorithm SHA256
```

A mismatch means the copy is damaged rather than dangerous — a file this size across a
network or a USB stick does occasionally arrive short, and a damaged executable can fail in
ways that look like a configuration problem. Copy it across again rather than running what
you have.

What this proves is that the file arrived exactly as it was built. It is not a signature and
says nothing about who built it: anyone able to alter the zip could rewrite the checksum
beside it. Only a code-signing certificate answers that question, and DdnsUpdate does not
have one.
