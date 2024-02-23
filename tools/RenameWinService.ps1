# define parameters
param (
    [Parameter(Mandatory=$true)]
    [string]$path,
    
    [Parameter(Mandatory=$true)]
    [string]$oldString = "WinService",
    
    [Parameter(Mandatory=$true)]
    [string]$newString,
    
    [string[]]$fileTypes = @(".cs", ".csproj", ".sln"), # don't update *.md
    
    [string[]]$ignoreDirs = @("obj", "bin")
)

function RenameFileContents {
    param (
        [string]$dirPath
    )

    # Get all the items in the current directory
    $items = Get-ChildItem -Path $dirPath

    foreach ($item in $items) {
        if ($item.PSIsContainer) {
            # the item is a directory

            # check if the directory is in the ignore list
            if ($ignoreDirs -notcontains $item.Name) {
                # the directory is not in the ignoreDirs list, so we recurse into it
                RenameFileContents -dirPath $item.FullName
            }
        } else {
            # check if the file has one of the specified extensions
            if ($fileTypes -contains $item.Extension) {
                # the file has one of the specified extensions

                # the item is a file
                "Updating file $($item.FullName)" | Write-Host

                # read the content of the file
                $content = Get-Content $item.FullName

                # replace the old string with the new string
                $content = $content -replace $oldString, $newString

                # write the new content to the file in utf-8 format
                Set-Content -Path $item.FullName -Value $content -Encoding UTF8
            }
        }
    }
}

function RenameFiles {
    param (
        [string]$dirPath
    )

    # check if the current directory is in the ignore list
    if ($ignoreDirs -contains (Split-Path -Leaf -Path $dirPath)) {
        "Ignoring directory $dirPath" | Write-Host
        return
    }

    # get all files in the current directory with the current file type
    $files = Get-ChildItem -Path $dirPath

    foreach ($file in $files) {
        # check if the file name contains the old substring
        if ($file.Name -match $oldString) {
            # replace the old substring with the new one
            $newName = $file.Name -replace $oldString, $newString
            # rename the file
            Rename-Item -Path $file.FullName -NewName $newName
            "Renamed $($file.FullName) to $newName" | Write-host
        }
    }

    # get all subdirectories in the current directory
    $dirs = Get-ChildItem -Path $dirPath -Directory

    foreach ($dir in $dirs) {
        # recursively call this function on each subdirectory
        RenameFiles -dirPath $dir.FullName
    }
}

"`nReplacing file contents: $oldString with $newString" | Write-Host
RenameFileContents -dirPath $path

"`nReplacing file names: $oldString with $newString" | Write-Host
RenameFiles -dirPath $path
