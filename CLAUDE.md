# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

DdnsUpdate is a Windows app that detects the machine's external IPv4 address and pushes it to Cloudflare DNS records. The same executable runs as a Windows Service, a Scheduled Task, or an interactive console app. It was derived from the `paultechguy/WinService.Net` template.

**Which line this is:** this tree descends from the `v0.1.0` production line (plus a Cloudflare PATCH fix) and is now 0.2.0 (see `CHANGELOG.md`; the version is set in `Directory.Build.props`). The GitHub `master` branch is an unrelated history with no common ancestor. It reads settings from a `config\` subfolder, names its exe `ddnsupdate.exe` in lowercase, and also reports 0.1.0. Don't port `config\`-folder behavior into this tree. See `docs/BUILDING.md`.

**Compatibility rule:** existing server settings files (especially `appsettings.production.user.json`) must keep working unchanged, so a deployment is just replacing `DdnsUpdate.exe`. Don't rename or restructure settings keys; new settings must be optional with defaults. The key `randomizeIpAddressProviderSelecion` is misspelled on purpose.

## Build setup

- .NET 10. `global.json` pins the SDK. Libraries target `net10.0` (from the root `Directory.Build.props`), and the exe targets `net10.0-windows10.0.17763.0`.
- `Directory.Build.props` also sets nullable, implicit usings, version, company, StyleCop.Analyzers and `EnforceCodeStyleInBuild` for every project.
- Package versions live only in the root `Directory.Packages.props` (central package management). `PackageReference` items have no `Version`.
- Analyzer rules are in `src/.editorconfig`; there is no separate lint step. Builds should have zero warnings.

## Commands

All commands run from the repository root. `DdnsUpdate.slnx` is the only solution, so `dotnet build` and `dotnet test` find it.

```powershell
dotnet build
dotnet test

# Run locally (Debug copies appsettings.development*.json → development environment)
dotnet run --project src\DdnsUpdate.Application

# Production single-file publish to .\publish (profile in src\DdnsUpdate.Application\Properties\PublishProfiles)
dotnet publish src\DdnsUpdate.Application -p:PublishProfile=win-x64-single
```

Layout: `src/` holds the six app projects, `tests/` the test projects, and `docs/` the build and deploy notes. Shared build files (`Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.editorconfig`) are at the root.

Tests use xUnit v3 on Microsoft Testing Platform (opted in via `global.json`'s `test.runner`; no VSTest packages).
- `tests/Directory.Build.props` imports the root props, adds xUnit, and compiles the fakes in `tests/Shared/` into every test project: `TestOptionsMonitor`, `FakeHttpMessageHandler` and `StubHttpClientFactory`.
- `WorkerService` tests use in-memory fakes (`tests/DdnsUpdate.Service.Tests/Fakes.cs`) and a `FakeTimeProvider`, so they never touch ProgramData or the network. Tests with `maximumDdnsUpdateIterations = 1` never advance the clock, so they would hang if the worker waited before exiting.
- Run one test with `dotnet test --project tests\DdnsUpdate.Service.Tests -- --filter-method "*UnchangedIp_SkipsUpdates"`.
- Pass `TestContext.Current.CancellationToken` to anything that takes a token. xUnit1051 warns otherwise.
- To smoke-test the Debug exe, run it with `--dry-run` or `--once` (defined in `CommandLineOptions`).
  - `--dry-run` is one read-only pass. It detects the IP, validates each domain, and calls `GetDnsRecordAsync` (a Cloudflare GET) to prove the credentials and IDs. It never PATCHes, saves state, or emails, and it exits with 1 if any domain has a problem.
  - `--once` is one real pass. Without either option, the app sleeps 60 minutes between passes.
  - Don't pass settings as command-line arguments. CommandLineParser rejects unknown arguments, and the app then exits with code 1 without running. Use environment variables instead, e.g. `cloudflareSettings__domains__0__isEnabled=true` to enable a fake domain.
  - A run writes logs to `%ProgramData%\PaulTechGuy\DdnsUpdate`. A dry run writes only logs there.
- The public site is `docs/index.html` (plus `docs/assets/`, `docs/sitemap.xml` and `docs/.nojekyll`), served by GitHub Pages from `master:/docs`. It is static HTML/CSS with no build step. Keep its claims in step with the README.
- The publish must be single-file. `FilePathHelper` and `Program.Main` use `AppDomain.CurrentDomain.BaseDirectory` because `Assembly.Location` is empty in a bundle.
- Building from a deeply nested path can hit the 260-character limit. That shows up as a misleading `MSB9008` "referenced project does not exist" warning.

## Architecture

Project dependency flow: `Application` → `Service`, `Email`, `DdnsProvider.Cloudflare`. `Service` and `Email` depend on `Core`. `Service` and `DdnsProvider.Cloudflare` depend on the `DdnsProvider` abstraction; `Service` never references the Cloudflare implementation.

Each library registers its own services through an `IServiceCollection` extension: `AddDdnsUpdateWorker()`, `AddSmtpEmailSender()` and `AddCloudflareDdnsProvider(configuration)`. `Program_Configure.ConfigureServices` composes them, and that is where you swap DDNS providers.

- **DdnsUpdate.Application** (assembly name `DdnsUpdate`): the entry point, a `partial class Program` split across `Program.cs` and `Program_Configure.cs`.
  - `Main` sets the current directory to the exe directory, then picks the environment. It uses `DOTNET_ENVIRONMENT` if set, and fails if the matching appsettings file is missing. Otherwise it looks for `appsettings.development.json`, then `appsettings.production.json`.
  - It builds a `HostApplicationBuilder` with `DisableDefaults = true`, then adds exactly these config sources, in order: `appsettings.json` → `appsettings.{env}.json` → `appsettings.{env}.user.json` (all reload on change) → environment variables → command line.
  - `*.user.json` files hold secrets (Cloudflare keys, zone and record IDs, SMTP credentials). They are gitignored and live only on the server.
  - `ApplicationSettings` is validated at startup (`ValidateOnStart`: `ipAddressProviders` must hold absolute http(s) URLs).
  - Serilog writes to `%ProgramData%\PaulTechGuy\DdnsUpdate\logs`, plus the console when interactive, and the `Polly` category is raised to Warning.
    - The `Serilog` sections in the appsettings files are **not read** (there is no `ReadFrom.Configuration`), so the minimum level is effectively Information.
    - Turning those sections on would silence Information logs in production, which sets Warning.
  - Exit code: 0 on a normal finish (including after `maximumDdnsUpdateIterations`), 1 on startup failure, a bad argument, or a worker exception. `WindowsBackgroundService` sets `Environment.ExitCode` for the worker case.
- **DdnsUpdate.Core**:
  - `WindowsBackgroundService` is the hosted service. It runs `IWorkerService.ExecuteAsync`, and always calls `StopApplication` afterwards, so the host never lingers as a zombie.
  - Core also holds the shared settings models, `FilePathHelper` (the app data dir), and the empty `CommandLineOptions` (CommandLineParser).
- **DdnsUpdate.Service**: `WorkerService` holds the main loop. On each iteration it:
  1. Snapshots `IOptionsMonitor<ApplicationSettings>.CurrentValue`, so settings file edits apply on the next pass.
  2. Asks the provider for the enabled domains. The provider snapshots its own settings here.
  3. Gets the external IP from the `ipAddressProviders` URL list. It uses the named client `IpAddressProvider` (15-second timeout), starts at a random provider, and rotates on failure. Per-URL success and failure counts persist to `UriStatistics.json`.
  4. Compares the result with `LastIpAddress.txt`. If the IP is unchanged it skips the update, unless `alwaysUpdateDdnsEvenIfUnchanged` is set.
  5. Optionally sends email.
  6. Updates the domains with `Parallel.ForEachAsync` (`parallelDdnsUpdateCount`), calling `IsDomainValidAsync` before each `TryUpdateIpAddressAsync`.
  7. Waits `afterAllDdnsUpdatePauseMinutes` using `Task.Delay` with the injected `TimeProvider`.

  `maximumDdnsUpdateIterations > 0` makes it exit after N loops, which is how Scheduled Task mode works. The check runs before the wait, so the process exits immediately after its last pass.
- **DdnsUpdate.DdnsProvider**: the `IDdnsUpdateProvider` abstraction. Providers own their HTTP clients and report failures in `DdnsProviderSuccessResult` rather than throwing.
- **DdnsUpdate.DdnsProvider.Cloudflare** implements it.
  - It binds `IOptionsMonitor<CloudflareSettings>` to the `cloudflareSettings` section, where a per-domain empty field falls back to `defaultDomain`.
  - It sends a **PATCH** (not PUT) to `zones/{zoneId}/dns_records/{recordId}` on the named client `Cloudflare`, which has a base address and the standard resilience handler (retries).
  - Credentials are either `apiToken` (sent as `Authorization: Bearer`) or `authorizationKey` plus `authorizationEmail` (sent as `X-Auth-Key`/`X-Auth-Email`).
  - `GetSettingsCredentials` resolves them: a domain's own token or key beats `defaultDomain`, and at each level a token beats a key.
  - Auth headers are set on each request, not on the client, because updates run in parallel.
  - PUT resets omitted fields such as `proxied`, which silently turns off the Cloudflare proxy. Keep it PATCH.
- **DdnsUpdate.Email**: an `IEmailSender` using MailKit.
  - It reads `applicationSettings.emailSmtpSettings` through `IOptionsMonitor` at send time.
  - `smtpEnableSsl` maps to STARTTLS, or to implicit TLS when the port is 465; with it off there is no TLS.
- **State**: `IDdnsStateStore` (Core) has `FileDdnsStateStore` (Service) as its production implementation. It owns `LastIpAddress.txt` and `UriStatistics.json`, so the worker never touches files directly.

Runtime data (logs, `LastIpAddress.txt`, `UriStatistics.json`) lives in `%ProgramData%\PaulTechGuy\DdnsUpdate`, not beside the exe.

## Code style

- C# uses **3-space indentation** (set in `.editorconfig`).
- Save `.cs` files as UTF-8 **with BOM** and **no trailing newline**. StyleCop warns otherwise (SA1412, SA1518).
- Every `.cs` file starts with the `PaulTechGuy` copyright/MIT header block. Put `using` directives inside the file-scoped namespace, with System first.
- Always qualify members with `this.` (enforced as a warning). Use primary constructors that assign to `private readonly` fields.
- Discard unused return values with `_ =` (e.g. `_ = services.AddTransient<...>()`).
- Log with constant message templates and PascalCase placeholders, not interpolated strings (CA2254 is a warning).
  - Pass a `Uri` as `.ToString()`. Otherwise Serilog quotes it in the file log.
- Public types and members get XML doc comments. Inline comments explain why, not what.

## Deployment / verification notes (from docs/BUILDING.md)

- Stop the running updater before replacing `DdnsUpdate.exe`, and back up the directory. Settings stay flat beside the exe. Never overwrite `appsettings.production.user.json`.
- A normal run with an unchanged IP makes no Cloudflare calls. To exercise the API, temporarily set `alwaysUpdateDdnsEvenIfUnchanged: true`, run once, then revert it. Check that proxied records keep the orange cloud.
