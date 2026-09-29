# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

DdnsUpdate is a Windows app that detects the machine's external IPv4 address and pushes it to Cloudflare DNS records. The same executable runs as a Windows Service, a Scheduled Task, or an interactive console app. It was derived from the `paultechguy/WinService.Net` template.

The exe project targets `net10.0-windows10.0.17763.0`. The library projects still target `net8.0`. Package versions are managed centrally in the root `Directory.Packages.props`, so `PackageReference` items have no `Version`.

**Which line this is:** this tree is the `v0.1.0` production line plus a Cloudflare PATCH fix. The GitHub `master` branch is an unrelated history with no common ancestor. It reads settings from a `config\` subfolder and names its exe `ddnsupdate.exe` in lowercase. Both lines report version 0.1.0. Don't port `config\`-folder behavior into this tree. See `docs/BUILDING.md`.

## Commands

All commands run from `src/`.

```powershell
dotnet build .\DdnsUpdate.sln

# Run locally (Debug copies appsettings.development*.json → development environment)
dotnet run --project .\DdnsUpdate.Application

# Production single-file publish (required form; see docs/BUILDING.md)
dotnet publish .\DdnsUpdate.Application\DdnsUpdate.Application.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish
```

- There are no test projects.
- To smoke-test a single pass, set the environment variable `applicationSettings__ddnsSettings__maximumDdnsUpdateIterations=1` and run the Debug exe.
  - Without it, the app sleeps 60 minutes between passes.
  - Don't pass settings as command-line arguments. CommandLineParser rejects unknown arguments, and the app then exits without running.
  - Environment variables can also enable a fake domain, e.g. `cloudflareSettings__domains__0__isEnabled=true`. Leaving its IDs empty makes validation fail before any Cloudflare call.
  - A run writes to `%ProgramData%\PaulTechGuy\DdnsUpdate`.
- StyleCop.Analyzers runs at build time, configured in `src/.editorconfig`. There is no separate lint step.
- The publish must be single-file. `FilePathHelper` and `Program.Main` use `AppDomain.CurrentDomain.BaseDirectory` because `Assembly.Location` is empty in a bundle.
- Building from a deeply nested path can hit the 260-character limit. That shows up as a misleading `MSB9008` "referenced project does not exist" warning.

## Architecture

Project dependency flow: `Application` → `Service`, `Email`, `DdnsProvider.Cloudflare` → `Core` / `DdnsProvider`.

- **DdnsUpdate.Application** (assembly name `DdnsUpdate`): the entry point, a `partial class Program` split across `Program.cs` and `Program_Configure.cs`.
  - `Main` sets the current directory to the exe directory, then picks the environment. It uses `DOTNET_ENVIRONMENT` if set, and fails if the matching appsettings file is missing. Otherwise it looks for `appsettings.development.json`, then `appsettings.production.json`.
  - Config layering: `appsettings.json` → `appsettings.{env}.json` → `appsettings.{env}.user.json` → environment variables → command line.
  - `*.user.json` files hold secrets (Cloudflare keys, zone and record IDs, SMTP credentials). They are gitignored and live only on the server.
  - Serilog writes to `%ProgramData%\PaulTechGuy\DdnsUpdate\logs`, plus the console when interactive.
  - `Run` always ends with `Environment.Exit(1)` so that Windows Service recovery options fire.
- **DdnsUpdate.Core**: `WindowsBackgroundService` is the hosted service. It calls `IWorkerService.ExecuteAsync`, then stops the host. Core also holds shared settings models, `FilePathHelper` (the app data dir), and the empty `CommandLineOptions` (CommandLineParser).
- **DdnsUpdate.Service**: `WorkerService` holds the main loop. On each iteration it:
  1. Re-reads `applicationSettings` from `IConfiguration`. This is deliberate: it picks up edits made while the service runs, instead of using IOptions.
  2. Asks the provider for the enabled domains.
  3. Gets the external IP from the `ipAddressProviders` URL list, starting at a random provider and rotating on failure. Per-URL success and failure counts persist to `UriStatistics.json`.
  4. Compares the result with `LastIpAddress.txt`. If the IP is unchanged it skips the update, unless `alwaysUpdateDdnsEvenIfUnchanged` is set.
  5. Optionally sends email.
  6. Updates the domains with `Parallel.ForEachAsync` (`parallelDdnsUpdateCount`), calling `IsDomainValidAsync` before each `TryUpdateIpAddressAsync`.
  7. Sleeps `afterAllDdnsUpdatePauseMinutes`.

  `maximumDdnsUpdateIterations > 0` makes it exit after N loops, which is how Scheduled Task mode works. The check runs before the sleep, so the process exits immediately after its last pass.
- **DdnsUpdate.DdnsProvider**: the `IDdnsUpdateProvider` abstraction. **DdnsUpdate.DdnsProvider.Cloudflare** implements it.
  - It reads the `cloudflareSettings` section, where a per-domain empty field falls back to `defaultDomain`.
  - It sends a **PATCH** (not PUT) to `/zones/{zoneId}/dns_records/{recordId}` using the `X-Auth-Email`/`X-Auth-Key` headers. PUT resets omitted fields such as `proxied`, which silently turns off the Cloudflare proxy. Keep it PATCH.
  - The provider is registered in `Program_Configure.ConfigureServices`, which is where you swap providers.
- **DdnsUpdate.Email**: an SMTP `IEmailSender`, configured from `applicationSettings.emailSmtpSettings`.

Runtime data (logs, `LastIpAddress.txt`, `UriStatistics.json`) lives in `%ProgramData%\PaulTechGuy\DdnsUpdate`, not beside the exe.

## Code style

- C# uses **3-space indentation** (set in `.editorconfig`).
- Save `.cs` files as UTF-8 **with BOM** and **no trailing newline**. StyleCop warns otherwise (SA1412, SA1518).
- Public types and members get XML doc comments. Inline comments explain why, not what.
- Every `.cs` file starts with the `PaulTechGuy` copyright/MIT header block. Put `using` directives inside the file-scoped namespace, with System first.
- Always qualify members with `this.` (enforced as a warning). Use primary constructors that assign to `private readonly` fields.
- Discard unused return values with `_ =` (e.g. `_ = services.AddTransient<...>()`).

## Deployment / verification notes (from docs/BUILDING.md)

- Stop the running updater before replacing `DdnsUpdate.exe`, and back up the directory. Settings stay flat beside the exe. Never overwrite `appsettings.production.user.json`.
- A normal run with an unchanged IP makes no Cloudflare calls. To exercise the API, temporarily set `alwaysUpdateDdnsEvenIfUnchanged: true`, run once, then revert it. Check that proxied records keep the orange cloud.
