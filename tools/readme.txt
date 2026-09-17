This PowerShell script allows you to rename the WinService template to another name (e.g. CheckCpuService).
At some point I'll update this code to support a "dotnet new" template; I've found the documentation for
creating a multi-project template for "dotnet new" to be lacking at this point.

Note: Before running the RenameWinService script, make sure that your PowerShell execution policy
allows running scripts. You can check the current execution policy by running
Get-ExecutionPolicy, and set it to allow scripts with Set-ExecutionPolicy RemoteSigned
or Set-ExecutionPolicy Unrestricted. Be aware that this can have security implications,
so please understand what this means and consider the risks.

** EXAMPLE** 

ASSUMING YOU ARE IN THE SRC DIRECTORY, replace all occurrences of "WinService" with "CheckCpuService"
in the current directory and its subdirectories, ignoring directories named "bin", "obj":

PS> ..\tools\RenameWinService.ps1 -path "." -oldString "WinService" -newString "CheckCpuService" -fileTypes ".sln", ".csproj", ".cs"
