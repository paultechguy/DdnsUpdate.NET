$currentDirectory = Get-Location
$serviceName = "DdnsUpdate.NET"
$appFileName = "DdnsUpdate.exe"
$programFilesPath = $env:ProgramFiles
$programDataPath = $env:ProgramData

# ensure we're in app directory
if (-not (Test-Path -Path '.\windows_service_setup' -PathType Container)) {
  Write-Host '%Error: Run this script from the main app directory.'
  exit 1
}

function WarnUserToContinue {
    Write-Host "Notice: You are about to install the `"$serviceName`" service." -ForegroundColor Yellow
    $response = (Read-Host "This action will install application and configuration files.  Do you want to continue (yes/no)? ").ToLower()

    if ($response -ne "yes") {
        return $false
    } else {
        return $true
    }
}

# call the function from the main script
if (-not (WarnUserToContinue)) {
    exit 1
}

# service
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service -eq $null) {
    if (-not (Test-Path -Path $appFileName)) {
        Write-Host "%Errog: $appFileName not found."
        exit 1
    }
    $exeFileFullPath = Resolve-Path -Path $appFileName
    sc.exe create $serviceName binpath="$exeFileFullPath" > $null
    sc.exe config $serviceName start=delayed-auto
}
else {
    Write-Host "%Error: The `"$serviceName`" is already installed."
    exit 1
}

# main application
$appDir = Join-Path -Path $programFilesPath -ChildPath "DdnsUpdate"
if (-Not (Test-Path -Path $appDir)) {
    New-Item -ItemType Directory -Path $appDir > $null
}

$appPath = Join-Path -Path $appDir -ChildPath $appFileName
if (-Not (Test-Path -Path $appPath)) {
    Copy-Item $exeFileFullPath -Destination $appPath
}

# config dir and files
$appDataDir = Join-Path -Path $programDataPath -ChildPath "DdnsUpdate"
if (-Not (Test-Path -Path $appDataDir)) {
    New-Item -ItemType Directory -Path $appDataDir > $null
}

$configDir = Join-Path -Path $appDataDir -ChildPath "config"
if (-Not (Test-Path -Path $configDir)) {
    New-Item -ItemType Directory -Path $configDir > $null
    Copy-Item ".\config\*.*" -Destination $configDir
}

# log dir
$logDir = Join-Path -Path $appDataDir -ChildPath "logs"
if (-Not (Test-Path -Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir > $null
}

# data dir
$dataDir = Join-Path -Path $appDataDir -ChildPath "data"
if (-Not (Test-Path -Path $dataDir)) {
    New-Item -ItemType Directory -Path $dataDir > $null
}

# done
Write-Host "The `"$serviceName`" service is installed."
Write-Host "You may now manage the service using the commands below (with administrator priviledge):"
Write-Host "   sc.exe start DdnsUpdate.NET"
Write-Host "   sc.exe stop DdnsUpdate.NET"
Write-Host "   sc.exe query DdnsUpdate.NET"
