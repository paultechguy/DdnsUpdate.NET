$currentDirectory = Get-Location
$serviceName = "DdnsUpdate.NET"
$appFileName = "DdnsUpdate.exe"
$programFilesPath = $env:ProgramFiles
$programDataPath = $env:ProgramData

function WarnUserToContinue {
    Write-Host "Warning: You are about to uninstall the `"$serviceName`" service." -ForegroundColor Red
    $response = (Read-Host "This action will remove application, configuration, and log files.  Do you want to continue (yes/no)? ").ToLower()

    if ($response -ne "yes") {
        return $false
    } else {
        return $true
    }
}

# ensure we're in app directory
if (-not (Test-Path -Path '.\windows_service_setup' -PathType Container)) {
  Write-Host '%Error: Run this script from the main app directory'
  exit 1
}

# call the function from the main script
if (-not (WarnUserToContinue)) {
    exit 1
}

# service
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service) {
    if ($service.Status -eq 'Running') {
        sc.exe stop $serviceName > $null
    }
    
    sc.exe delete $serviceName > $null
    
    # wait a bit before we begin removing folders
    Write-Host 'Pausing while service is uninstalled...'
    Start-Sleep -Seconds 3
}
else {
    Write-Host "%Error: The `"$serviceName`" is not installed."
    exit 1
}

# main application
$appDir = Join-Path -Path $programFilesPath -ChildPath "DdnsUpdate"
if (Test-Path -Path $appDir) {
    Remove-Item $appDir -Recurse
}

# config dir and files, log dir, data dir
$appDataDir = Join-Path -Path $programDataPath -ChildPath "DdnsUpdate"
if (Test-Path -Path $appDataDir) {
    Remove-Item $appDataDir -Recurse
}

# done
Write-Host "The `"$serviceName`" service has been uninstalled."