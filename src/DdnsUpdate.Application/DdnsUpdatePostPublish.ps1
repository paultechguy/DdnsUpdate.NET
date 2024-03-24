param(
    [string]$OS,
    [string]$ProjectDir,
    [string]$TargetDir,
    [string]$PublishUrl
)

function EnsurePathEndsWithSlash {
    param (
        [string]$path
    )

    if (-not $path.EndsWith('\')) {
        $path += '\'
    }

    return $path
}

function EnsurePathEndsWithoutSlash {
    param (
        [string]$path
    )

    if ($path.EndsWith('\')) {
        $path = $path.Substring(0, $path.Length - 1)
    }

    return $path
}

# invoking from VS build causes extra quotes at end
$TargetDir = $TargetDir.Trim('"')
$ProjectDir = $ProjectDir.Trim('"')

# adjust to remove any ending slashes
$TargetDir = EnsurePathEndsWithoutSlash $TargetDir
$ProjectDir = EnsurePathEndsWithoutSlash $ProjectDir

$pluginNames = @('PaulTechGuy.Cloudflare', 'PaulTechGuy.EmailOnly')
$pluginNames | ForEach-Object {
    $sourcePath = $TargetDir.Replace('DdnsUpdate.Application', "plugins`\$_")
    $destPath = $TargetDir + "\plugins\$_"

        # adjust if dest is commented out
    if (-not (Test-Path -Path $destPath -PathType Container)) {
        $commentedDestPath = EnsurePathEndsWithSlash "$($TargetDir)\plugins\#$_"
        if (Test-Path -Path $commentedDestPath) {
            $destPath = $commentedDestPath
        }
    }
 
    if (-not (Test-Path -Path $destPath -PathType Container)) {
        New-Item -Path $destPath -ItemType "directory"
    }

    if ((Test-Path -Path $sourcePath -PathType Container) -and (Test-Path -Path $destPath -PathType Container)) {
        Copy-item -Force -Recurse "$($sourcePath)`\*" -Destination $destPath
    }
    else {
        Write-Host "missing source/dest folder"
    }
}

exit 0