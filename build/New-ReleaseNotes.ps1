#Requires -Version 7.0

<#
.SYNOPSIS
    Starts a release: bumps the version and scaffolds the release notes for you to write.

.DESCRIPTION
    The first of the two release steps, and the only one that produces something you have to
    think about. It changes two files in the working tree and stops:

        Directory.Build.props        <Version> set to the number you passed
        docs\releases\v<version>.md  scaffolded from build\release-notes-template.md

    Nothing is committed and nothing is pushed. You fill in the placeholders, then commit both
    files to develop as an ordinary change - reviewed the way any other change is reviewed. By
    the time Publish-Release.ps1 runs, "develop is ready to release" is a plain fact about
    develop rather than a state this script left behind.

    That ordering is the point. Release notes are the one part of a release that needs
    judgement, and judgement should not happen inside a script run with a half-finished
    release waiting on it.

    The gates here are the cheap ones - branch, tree, ancestry, and whether the version is
    still free. Scaffolding a markdown file has no business running the test suite; the full
    set runs in Publish-Release.ps1 where they actually gate something irreversible.

.PARAMETER Version
    The version to release, as three numbers: 0.3.0. Drives Directory.Build.props, the notes
    file name, and later the tag, the zip name and the release title.

.PARAMETER Force
    Overwrite an existing notes file for this version. The version bump is idempotent, so this
    is about throwing away notes you have already started.

.PARAMETER Check
    Run the gates and report, then stop. Nothing is written.

.PARAMETER Republish
    Allow a version that already has a draft release and a tag. Use it with -Force to rewrite
    the notes of a version you are about to rebuild with Publish-Release.ps1 -Republish; it
    changes nothing here beyond the tag gate's answer.

.EXAMPLE
    pwsh .\build\New-ReleaseNotes.ps1 -Version 0.3.0

.EXAMPLE
    pwsh .\build\New-ReleaseNotes.ps1 -Version 0.3.0 -Check

    Ask whether a release could start, without starting one.

.NOTES
    Exit codes: 0 success, 1 failure.
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [string] $Version,

    [switch] $Force,

    [switch] $Check,

    [switch] $Republish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'ReleaseCommon.ps1')

$repoRoot = Split-Path -Parent $PSScriptRoot
$buildProps = Join-Path $repoRoot 'Directory.Build.props'
$notesTemplate = Join-Path $PSScriptRoot 'release-notes-template.md'
$notesRelative = "docs/releases/v$Version.md"
$notesPath = Join-Path $repoRoot ($notesRelative -replace '/', [System.IO.Path]::DirectorySeparatorChar)

try {
    Assert-VersionString $Version

    Write-Host ''
    Write-Host "DdnsUpdate release notes $Version" -ForegroundColor White
    Write-Host ''

    # ---- gates
    Write-Host '  preflight' -ForegroundColor DarkGray

    # The two files this script is about to touch, and the changelog you edit alongside them,
    # are allowed to be dirty already, so that a second run with -Force works without making
    # you revert the first one by hand.
    Test-GateTree -RepoRoot $repoRoot -Branch $DdDevelopmentBranch -AllowDirty @('Directory.Build.props', 'CHANGELOG.md', $notesRelative)
    Test-GateAncestor -RepoRoot $repoRoot
    Test-GateTagFree -RepoRoot $repoRoot -Version $Version -Republish:$Republish
    Test-GateNotesAbsent -NotesPath $notesPath -Force:$Force

    Write-Host ''

    if ($Check) {
        Write-Host '  Nothing written. A release of ' -NoNewline -ForegroundColor DarkGray
        Write-Host $Version -NoNewline -ForegroundColor White
        Write-Host ' can start from here.' -ForegroundColor DarkGray
        Write-Host ''
        exit 0
    }

    Initialize-TaskList -Total 2

    # ---- version
    Write-Task 'version'
    $previous = Get-RepoVersion -PropsPath $buildProps

    if ($PSCmdlet.ShouldProcess($buildProps, "Set <Version> to $Version")) {
        $changed = Set-RepoVersion -PropsPath $buildProps -Version $Version
    }
    else {
        $changed = $false
    }

    if ($previous -eq $Version) {
        Write-Done "already $Version"
    }
    elseif ($changed) {
        Write-Done "$previous -> $Version"
    }
    else {
        Write-Done "$previous -> $Version (not written)"
    }

    # ---- notes
    Write-Task 'release notes'
    $scaffold = Expand-Template -Path $notesTemplate -Token @{ VERSION = $Version }

    if ($PSCmdlet.ShouldProcess($notesPath, 'Scaffold release notes')) {
        Save-Text -Path $notesPath -Text $scaffold
    }

    Write-Done $notesRelative

    # ---- what to do next
    $placeholders = ($scaffold -split '\r?\n' | Where-Object { $_ -match 'TODO' }).Count

    Write-Host ''
    Write-Host '  Next' -ForegroundColor White
    Write-Host "    Fill in the $placeholders placeholders in $notesRelative, deleting any heading" -ForegroundColor DarkGray
    Write-Host "    that has nothing under it. Add the same changes to CHANGELOG.md. Then commit" -ForegroundColor DarkGray
    Write-Host "    them to ${DdDevelopmentBranch}:" -ForegroundColor DarkGray
    Write-Host ''
    Write-Host "      git add Directory.Build.props CHANGELOG.md $notesRelative" -ForegroundColor Gray
    Write-Host "      git commit -m ""Release notes for $Version""" -ForegroundColor Gray
    Write-Host "      git push origin $DdDevelopmentBranch" -ForegroundColor Gray
    Write-Host ''
    Write-Host '    Then publish:' -ForegroundColor DarkGray
    Write-Host ''
    Write-Host "      pwsh .\build\Publish-Release.ps1 -Version $Version" -ForegroundColor Gray
    Write-Host ''
    Write-Host '  Changed your mind' -ForegroundColor White
    Write-Host ''
    Write-Host '      git checkout -- Directory.Build.props' -ForegroundColor Gray
    Write-Host "      Remove-Item $notesRelative" -ForegroundColor Gray
    Write-Host ''

    exit 0
}
catch {
    Write-Failure $_.Exception.Message
    exit 1
}
