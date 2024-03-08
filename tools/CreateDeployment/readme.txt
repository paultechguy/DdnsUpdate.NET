How to create a deployment package for GitHub.  After you publish a Windows or Linux build:

Windows
=======
& "C:\...\DdnsUpdate\tools\CreateDeployment.ps1" "C:\...\DdnsUpdate\src\DdnsUpdate.Application\bin\Release\net8.0\publish\win-x64" Windows 0.1.0

Linux
=======
& "C:\...\DdnsUpdate\tools\CreateDeployment.ps1" "C:\...\DdnsUpdate\src\DdnsUpdate.Application\bin\Release\net8.0\publish\linux-x64" Linux 0.1.0
