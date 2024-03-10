# create local package (clp)
param(
    [Parameter(Mandatory=$true)]
    [string]$publishPath,

    [Parameter(Mandatory=$true)]
    [ValidateSet("windows", "linux")]
    [string]$platform,

    [Parameter(Mandatory=$true)]
    [string]$version
)

# Convert publishPath to an absolute path
$publishPath = Resolve-Path $publishPath

# Check if the publishPath is an existing directory
if (!(Test-Path -Path $publishPath -PathType Container)) {
    Write-Host "The provided path is not an existing directory. Exiting..."
    exit 1
}

# Check if the version parameter is empty
if ([string]::IsNullOrEmpty($version)) {
    Write-Host "The version parameter is empty. Exiting..."
    exit 1
}

# Set the name file name of the main app
$platform = $platform.ToLower()
switch ($platform) {
    "linux" {
        $mainAppFile = "ddnsupdate"
    }
    "windows" {
        $mainAppFile = "ddnsupdate.exe"
    }
    default {
        echo "Invalid $platform when setting mainAppFile. Exiting ..."
    }
}

# Check if the required files and directories exist
if (!(Test-Path -Path (Join-Path -Path $publishPath -ChildPath $mainAppFile)) -or
    !(Test-Path -Path (Join-Path -Path $publishPath -ChildPath "service_setup")) -or
    !(Test-Path -Path (Join-Path -Path $publishPath -ChildPath "config")) -or
    !(Test-Path -Path (Join-Path -Path $publishPath -ChildPath "plugins"))) {
    Write-Host "ddnsupdate, service_setup, config or plugins are missing in $publishPath. Exiting..."
    exit 1
}

# Set publishName to the last part of the publishPath
$publishName = (Split-Path -Path $publishPath -Leaf).ToLower()

# Get the current working directory
$currentDirectory = Get-Location

# Set the name of the platform service type
switch ($platform) {
    "linux" {
        $serviceType = "daemon"
    }
    "windows" {
        $serviceType = "service"
    }
    default {
        echo "Invalid $platform when setting serviceType. Exiting ..."
    }
}

# Create a zip file
switch ($platform) {
    "linux" {
        # Define the tar.gz archive file path
        $archiveFilePath = Join-Path -Path $currentDirectory -ChildPath "ddnsupdate.$publishName.$version.tar.gz"

        # Change the current directory to the directory of the files (so we get relative paths in the gz file)
        Set-Location -Path $publishPath

        # Define the files and directories to be included in the tar.gz archive
        $files = @($mainAppFile, "config", "plugins", "${platform}_${serviceType}_setup")

        # Temporarily move the linux daemon setup directory with a relative path, move it up
        mv "service_setup/${platform}_${serviceType}_setup" ./
        mv ./service_setup ./service_setup_temp

        # Create the tar.gz file with relative paths
        tar -cvzf $archiveFilePath $files  2>&1 > $null

        # Restore the service setup directory
        mv ./service_setup_temp ./service_setup
        mv "${platform}_${serviceType}_setup" ./service_setup


        # Change the current directory back to the original directory
        Set-Location -Path $currentDirectory
    }
    "windows" {
        # Zip using the native PowerShell Compress-Archive command
        $archiveFilePath = Join-Path -Path $currentDirectory -ChildPath "ddnsupdate.$publishName.$version.zip"
        Compress-Archive -Path (Join-Path -Path $publishPath -ChildPath $mainAppFile), 
                              (Join-Path -Path $publishPath -ChildPath "config"), 
                              (Join-Path -Path $publishPath -ChildPath "plugins"),
                              (Join-Path -Path $publishPath -ChildPath "service_setup\${platform}_${serviceType}_setup")  -Force -DestinationPath $archiveFilePath  2>&1 > $null
    }
    default {
        echo "Invalid $platform when creating deployment file. Exiting ..."
        exit 1
    }
}

Write-Host "Zip file created at $archiveFilePath"

exit 0
