How to create a local deployment package for GitHub.*

After you perform a Visual Studio publish for Windows or Linux:

clp.ps1 <publishPath> <platform> <version>


Windows
=======
& "C:\...\DdnsUpdate\tools\LocalPackage\clp.ps1" "C:\...\DdnsUpdate\src\DdnsUpdate.Application\bin\Release\net8.0\publish\win-x64" Windows 0.1.0


Linux
=======
& "C:\...\DdnsUpdate\tools\LocalPackage\clp.ps1" "C:\...\DdnsUpdate\src\DdnsUpdate.Application\bin\Release\net8.0\publish\linux-x64" Linux 0.1.0


* This can be executed from any directory location. Preferably outside of the git repo
directory to avoid adding deployment packages to the repo.