#Requires -Version 7.0

<#
.SYNOPSIS
    Builds the DdnsUpdate release zip: one command, one file, ready to copy to a server.

.DESCRIPTION
    Publishes the app with the win-x64-single profile (one self-contained DdnsUpdate.exe),
    stages it beside its settings files and documents, and compresses the lot into

        build\artifacts\DdnsUpdate-<version>-win-x64.zip
        build\artifacts\DdnsUpdate-<version>-win-x64.zip.sha256

    What comes out is self-contained. The .NET runtime is inside the executable, so the server
    needs nothing installed first. The zip has no wrapper folder; its contents sit at the root,
    flat, exactly as the app expects to find them at run time:

        DdnsUpdate.exe                the application
        appsettings.json              IP address providers (shared defaults)
        appsettings.production.json   production settings template
        README.md                     setup and operation guide
        CHANGELOG.md                  what changed, release by release
        LICENSE.txt                   MIT

    No .pdb files: debug symbols are for whoever built it, not for the server running it.

    Tests are opt-in via -Test. Repackaging is something you do repeatedly while getting a
    release right, and paying for the full suite on every iteration only teaches you to stop
    running the script. Publish-Release.ps1 always passes -Test.

.PARAMETER Configuration
    Build configuration. Release by default, and there is rarely a reason to change it: the
    publish profile and the environment detection both assume Release (a Release build ships
    appsettings.production.json, which is what makes the app run as production).

.PARAMETER Test
    Runs the test suite before publishing and stops if anything fails. Worth it for a release
    you are actually going to deploy.

.PARAMETER OutputDirectory
    Where the zip is written. Defaults to build\artifacts, which is git-ignored.

.PARAMETER KeepStaging
    Leaves the staged folder in place next to the zip, which is the quickest way to inspect
    exactly what shipped without unzipping it again.

.PARAMETER ShowBuildOutput
    Streams the dotnet output instead of capturing it. The output is shown automatically when
    a step fails; this is for when a step succeeds and you still want to see it.

.EXAMPLE
    pwsh .\build\New-Release.ps1

    The usual invocation.

.EXAMPLE
    pwsh .\build\New-Release.ps1 -Test

    The same, gated on a green test run.

.NOTES
    Exit codes: 0 success, 1 failure.
#>

[CmdletBinding()]
param(
    [string] $Configuration = 'Release',

    [switch] $Test,

    [string] $OutputDirectory,

    [switch] $KeepStaging,

    [switch] $ShowBuildOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Progress output, sizes and the dotnet wrapper are shared with New-ReleaseNotes.ps1 and
# Publish-Release.ps1, so they live in one file rather than three.
. (Join-Path $PSScriptRoot 'ReleaseCommon.ps1')

$repoRoot = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $repoRoot 'src\DdnsUpdate.Application\DdnsUpdate.Application.csproj'
$solution = Join-Path $repoRoot 'DdnsUpdate.slnx'
$buildProps = Join-Path $repoRoot 'Directory.Build.props'

$artifacts = if ($OutputDirectory) { [System.IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $PSScriptRoot 'artifacts' }
$staging = Join-Path $artifacts '.stage'
$runtimeIdentifier = 'win-x64'

Initialize-TaskList -Total $(if ($Test) { 4 } else { 3 })

try {
    Write-Host ''
    Write-Host 'DdnsUpdate release' -ForegroundColor White
    Write-Host ''

    # ---- tests
    if ($Test) {
        Write-Task 'tests'
        Invoke-Dotnet -Arguments @('test', '--solution', $solution, '-c', $Configuration) -FailureMessage 'Tests failed.' -Stream:$ShowBuildOutput
        Write-Done 'passed'
    }

    # ---- publish
    Write-Task "publish $Configuration"

    if (Test-Path -LiteralPath $staging) {
        Remove-Item -LiteralPath $staging -Recurse -Force
    }

    New-Item -ItemType Directory -Path $staging -Force | Out-Null
    $publishDir = Join-Path $staging 'publish'

    # The profile carries the single-file, self-contained, win-x64 settings; -o overrides only
    # its output folder, so a release build never lands in (or collides with) the developer's
    # own publish\ folder at the repository root.
    Invoke-Dotnet `
        -Arguments @('publish', $appProject, '-p:PublishProfile=win-x64-single', '-c', $Configuration, '-o', $publishDir, '--nologo') `
        -FailureMessage 'The publish failed.' `
        -Stream:$ShowBuildOutput

    $publishedExe = Join-Path $publishDir 'DdnsUpdate.exe'

    # Single file is a requirement, not a preference: FilePathHelper and Program rely on
    # AppDomain.CurrentDomain.BaseDirectory, which is only the exe's folder in a bundle. A
    # loose DdnsUpdate.dll beside the exe means the profile did not apply.
    foreach ($required in @('DdnsUpdate.exe', 'appsettings.json', 'appsettings.production.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $publishDir $required) -PathType Leaf)) {
            Write-Failed
            throw "The publish completed but produced no '$required' in '$publishDir'."
        }
    }

    if (Test-Path -LiteralPath (Join-Path $publishDir 'DdnsUpdate.dll')) {
        Write-Failed
        throw 'The publish produced a loose DdnsUpdate.dll, so it is not single-file. Check the win-x64-single publish profile.'
    }

    # A development settings file in a release would make the app run as development on the
    # server (Program picks the environment from which file exists).
    if (Test-Path -LiteralPath (Join-Path $publishDir 'appsettings.development.json')) {
        Write-Failed
        throw 'The publish contains appsettings.development.json; the app would run as development. Build with -Configuration Release.'
    }

    Write-Done (Format-Size (Get-Item -LiteralPath $publishedExe).Length)

    # ---- stage
    Write-Task 'stage'
    $version = Get-PublishedVersion -Exe $publishedExe -PropsPath $buildProps
    $stagedRoot = New-StagedRelease `
        -PublishDir $publishDir `
        -RepoRoot $repoRoot `
        -Version $version `
        -StagingRoot $staging `
        -RuntimeIdentifier $runtimeIdentifier
    Write-Done "v$version, $($DdReleaseEntries.Count) files"

    # ---- zip
    Write-Task 'zip'
    New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
    $zipPath = Join-Path $artifacts ("DdnsUpdate-$version-$runtimeIdentifier.zip")
    New-ReleaseArchive -StagedRoot $stagedRoot -ZipPath $zipPath

    $zipSize = (Get-Item -LiteralPath $zipPath).Length
    Write-Done (Format-Size $zipSize)

    # A checksum beside the zip, so whoever receives it can confirm it arrived intact.
    # Cheap to produce and the only thing this build can offer in place of a signature.
    $hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
    "$hash  $(Split-Path -Leaf $zipPath)" | Set-Content -LiteralPath "$zipPath.sha256" -Encoding ASCII

    if (-not $KeepStaging) {
        Remove-Item -LiteralPath $staging -Recurse -Force
    }

    Write-Host ''
    Write-Host "  $zipPath" -ForegroundColor White
    Write-Host "  SHA256 $hash" -ForegroundColor DarkGray
    Write-Host ''
    Write-Host '  New server: extract it into a folder and follow README.md.' -ForegroundColor DarkGray
    Write-Host '  Upgrade: stop the updater and replace only DdnsUpdate.exe.' -ForegroundColor DarkGray
    Write-Host ''

    exit 0
}
catch {
    Write-Failure $_.Exception.Message
    exit 1
}
