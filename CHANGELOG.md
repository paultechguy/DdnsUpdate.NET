# Changelog

All notable changes to this project are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.2.0] - Unreleased

Existing settings files, including `appsettings.production.user.json`, work
unchanged. Upgrading means replacing `DdnsUpdate.exe`.

### Added
- Cloudflare API token support (`apiToken` on a domain or on `defaultDomain`),
  sent as `Authorization: Bearer`. Tokens can be limited to DNS edits in
  specific zones, unlike the Global API Key, which remains supported.
- Implicit TLS for SMTP on port 465.
- Automatic retries with backoff for transient Cloudflare API failures.
- Startup validation of `ipAddressProviders`.
- Unit tests (`dotnet test`).

### Changed
- Runs on .NET 10 (LTS). .NET 8 support ends November 2026.
- Exit code is 0 after a normal run and 1 on failure. It was always 1, so Task
  Scheduler now shows success as `0x0`.
- Settings changes, including the SMTP server, apply on the next pass without
  a restart.
- Email is sent with MailKit instead of the deprecated `System.Net.Mail.SmtpClient`.
  A server without `smtpUsername` is now contacted without authentication,
  rather than with the service account's Windows credentials.
- Each domain's configuration is checked before updating, and every missing value
  is reported in one `Invalid configuration` log entry.
- IP address providers time out after 15 seconds instead of 100.
- The file log records exception details and no longer wraps some values in quotes.
- Repository layout: `DdnsUpdate.slnx` at the root, projects in `src/`, tests in
  `tests/`, and shared build settings in `Directory.Build.props` and
  `Directory.Packages.props`.

### Fixed
- With `maximumDdnsUpdateIterations` set (Scheduled Task mode), the application
  waited a full pause interval, 60 minutes by default, before exiting after its
  last pass.
- A missing or zero `afterAllDdnsUpdatePauseMinutes` paused for 60,000 minutes
  instead of the intended one minute.
- Domain updates were started but not awaited, so the process could exit before
  they finished.
- An HTTP client was leaked on every IP address lookup.
- `messageReplyToEmailAddress` was ignored.
- After an unexpected error, the service could keep running with nothing to do
  instead of exiting so Windows could restart it.
- README: corrected the email setting names (`emailSmtpSettings`, `smtpHost`, ...).

## [0.1.0] - 2024-02-23

Production release (tag `e2252fd`), plus a fix that updates Cloudflare records
with PATCH instead of PUT. PUT turned off the Cloudflare proxy on proxied records.
