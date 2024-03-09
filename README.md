# DdnsUpdate.NET (CloudFlare)

The primary use case for this application is to update Cloudflare DNS records with an externally visible IP address.  It will allow you to continuously update your Cloudflare DNS records using the Cloudflare dynamic DNS (DDNS) API.  The application can be used on Windows or Linux.

## Terminology
For the purpose of this document, the use of the word *application* refers to the DdnsUpdate.NET application.

## Requirements
- Cloudflare account with one or more domains
- Windows 10 or higher (Windows-x64) or Linux Ubuntu (Linux-x64)
- Visual Studio 2022 with .NET 8 (if you want to review the source code)

## Take A Test Drive
Before you configure the application to update your Cloudflare DNS records with our external IP address, you should test the application to make sure it works with the default configuration.  Doing this will ensure the configuration is correct and features such as logging are working.

Unpack the deployment file into a directory.  The directory will contain the following content:

### Windows
    config (directory)
    plugins (directory)
    windows_service_setup (directory)
    ddnsupdate.exe

### Linux
    config (directory)
    plugins (directory)
    linux_daemon_setup (directory)
    ddnsupdate

> Note: For the purposed of this document, the application file, `ddnsupdate`, refers to both the Linux application file or the Windows application file (`ddnsupdate.exe`).

To test the application:
1. Open a command window
1. Ensure the current directory is the location of the unpacked files
1. Enter the command, `ddnsupdate`

> Note: To execute this command on Linux, you might need the permissions on the application file as well as the configuration files.  

    chmod 0755 ddnsupdate
    chmod 0755 config/*

If everything is working correctly, you should see similar output to:

    [13:03:20 INF] PRODUCTION environment detected
    [13:03:20 INF] Starting DdnsUpdate.NET by PaulTechGuy, v0.1.0
    [13:03:20 INF] Press Ctrl-C to cancel
    [13:03:20 INF] IP address updates will be performed every 60 minute(s)
    [13:03:20 INF] IP address updates will not push email notifications
    [13:03:20 INF] config files are in C:\Users\...\config
    [13:03:20 INF] log files are in C:\Users\...\logs
    [13:03:20 INF] data files are in C:\Users\...\data
    [13:03:20 INF] Checking for initial IP address: none found
    [13:03:20 INF] #1: No enabled domains found; skip DNS update(s)

To stop the application, press Ctrl-C.

## Configuration
Once you have confirmed the application executes successfully out-of-the-box, you can configure it to update your Cloudflare domains. This is done by editing the `domains` JSON property in the `appsettings.production.json` file in the `config` directory.  By default, this property contains an example DNS update configuration that is disabled.

    "domains": [
        {
            "isEnabled": false,
            "name": "",
            "recordId": "",

            // optionally leave blank and use domain defaults
            "zoneId": "",
            "recordType": "",
            "authorizationKey": "",
            "authorizationEmail": ""
        }
    ]

To add DNS update configurations, you will need to gather some information from your Cloudflare account.  For each domain, you will need the `Domain name`, `zone ID`, and `record ID`.  In addition, for authenticating with the Cloudflare API, you will need your `authorization email` and `authorization key`.  Using your Cloudflare account, you can obtain these values:

1.  Domain Name: This is the official domain name (e.g. mycompany.com).
1.  Authorization Email: This is the email used for your account.
1.  Authorization Key: Navigate to the *API Tokens* tab and view your *Global API Key*.
1.  Zone ID: Navigate to the domain name.  On the *Overview* tab, you can view the Zone ID in the lower-right panel.
1.  Record ID: This can be a bit difficult to find.  There are a few methods.
    1. Navigate to the *Manage Account* tab and view the Audit Log. If you have recently edited a domain name in your account, you can view that specific log entry.  It should contain a reference to the record ID.
    1. You can use the Cloudflare API to get the record ID.  Using a *curl* command or a tool such as [Postman](https://www.postman.com/), submit a GET request.  You should be able to obtain the record ID from the result. Here is the format of an example *curl** command (replace the \{...\} tags with the required values):

          curl --location 'https://api.cloudflare.com/client/v4/zones/\{zoneId\}/dns_records' \\
        --header 'Content-Type: application/json' \\
        --header 'X-Auth-Email: \{authEmail\}' \\
        --header 'X-Auth-Key: \{authKey\}'

Using these five values, you can now configure your DNS configuration(s):

    "domains": [
        {
            "isEnabled": true,
            "name": "mycompany.com",
            "recordId": "{recordId}",

            // optionally leave blank and use domain defaults
            "zoneId": "{zoneId}",
            "recordType": "A",
            "authorizationKey": "{authKey}",
            "authorizationEmail": "{authEmail}"
        }
    ]

If you have more than one DNS record to configure, you can also enter default values in the `defaultDomain` property, and leave them blank in `domains`:

    "defaultDomain": {
        "zoneId": "",
        "recordType": "A",
        "authorizationKey": "{authKey}",
        "authorizationEmail": "{authEmail}"
    },
    "domains": [
        {
            "isEnabled": true,
            "name": "mycompany.com",
            "recordId": "{recordId}",

            // optionally leave blank and use domain defaults
            "zoneId": "{zoneId}",
            "recordType": "",
            "authorizationKey": "",
            "authorizationEmail": ""
        }
    ]

>Note: In this example, we opted to enter the zone ID in `domains` property rather than the defaults. This is because zone IDs tend to be specific for different domains.

You can now test the application to determine if the new configuration values are correct, and if your Cloudflare DNS records are updating correctly.  Executing the application in a command window should indicate your domain(s) are updated:

    [13:16:16 INF] PRODUCTION environment detected
    [13:16:16 INF] Starting DdnsUpdate.NET by PaulTechGuy, v0.1.0
    [13:16:16 INF] Press Ctrl-C to cancel
    [13:16:16 INF] IP address updates will be performed every 60 minute(s)
    [13:16:16 INF] IP address updates will not push email notifications
    [13:16:16 INF] config files are in C:\Users\...\config
    [13:16:16 INF] log files are in C:\Users\...\logs
    [13:16:16 INF] data files are in C:\Users\...\data
    [13:16:16 INF] Checking for initial IP address: none found
    [13:16:17 INF] New IP address found: 92.119.63.103
    [13:16:17 WRN] Email support disabled.  See appSettings.WorkerServiceSettings.MessageIsEnabled
    [13:16:17 INF] #1: Current external IP is 92.119.63.103 via URL https://api.ipify.org/
    [13:16:17 INF] #1: Processing IP updates for 1 domain(s)
    [13:16:17 INF] #1: Domain mycompany.com, IP updated to 92.119.63.103

When executing the application in a command window, you can view log files, the last known external IP address, and a statistics file in the application directory:

     logs (directory)
     LastIpAddress.txt
     UriSatistics.json

## Execute as a Windows Service
To execute the application as a Windows Service, you first install the service. Once the service is installed, you can manage it (e.g. start, stop) it using either the Windows Services interface or within a command window using the `sc.exe` command.

### Install the Windows Service
Please note that when the application is installed as a service, the files in the unpacked application directory are copied to other well-known system directories.  If you want to change a DNS configuration in the `appsettings.production.json` file, you would need to update the file in the service location.  The service locations are:

    ddnsupdate => C:\Program Files\DdnsUpdate
    config => C:\ProgramData\DdnsUpdate\config
    data => C:\ProgramData\DdnsUpdate\data
    logs => C:\ProgramData\DdnsUpdate\logs

To install the service, open a Windows PowerShell window, with administrator privileges, and run the installation script:

    & .\windows_service_setup\service_install.ps1

> Note that by default, the installed service is configured to automatically start when Windows starts up.

To uninstall the service, run the uninstall script:

    & .\windows_service_setup\service_uninstall.ps1

To start, stop, and get the service status, use the Windows `sc.exe` command:

    sc.exe start DdnsUpdate.NET
    sc.exe stop DdnsUpdate.NET
    sc.exe query DdnsUpdate.NET

You can view the application logs in the `C:\ProgramData\DdnsUpdate\logs` directory.

## Execute as a Linux Daemon
To execute the application as a Linux Daemon, you first install the daemon. Once the daemon is installed, you can manage it (start, stop) it using the `systemctl` command.

### Install the Linux Daemon
Please note that when the application is installed as a daemon, the files in the unpacked application directory are copied  to other well-known system directories. If you want to change a DNS configuration in the `appsettings.production.json` file, you would need to update the file in the daemon location.  The daemon locations are:

    ddnsupdate => /usr/sbin
    config => /etc/ddnsupdate
    data => /var/lib/ddnsupdate
    logs => /var/log/ddnsupdate

> Note: To execute the installation script, you might need the permissions on the script:
> `chmod 0755 linux_daemon_setup/*`

To install the daemon, open a command window, and run the installation script:

    sudo ./linux_daemon_setup/daemon_install.sh

> Note that by default, the installed daemon is configured to automatically start when Linux starts up.

To uninstall the daemon, run the uninstall script:

    sudo ./linux_daemon_setup/daemon_uninstall.sh

To start, stop, and get the daemon status, use the Linux `systemctl` command:

    sudo systemctl start ddnsupate.service
    sudo systemctl stop ddnsupate.service
    sudo systemctl status ddnsupate.service

You can view the application logs in the `/var/log/ddnsupdate` directory.

## DNS Update Interval
The default functionality is for the application to loop every sixty (60) minutes and update DNS records with the most recent external IP address. You can change this behavior by modifying the number of minutes to pause after each DDS update. This is found in the `appsettings.production.json` file.

    "ddnsSettings": {
        "afterAllDdnsUpdatePauseMinutes": 60,
        ...
    },

## Email Notifications
By default, the application will not send an email when the external IP address changes. If you want to be notified when the external IP address changes, you can enable this by editing the `appsettings.production.json` file.

To enable email, you will need to access to an external SMTP email server.  Once an SMTP email server is available, complete the following configuration steps:

1) Update the `appsettings.production.json` file as indicated below:

        "workerServiceSettings": {
          "messageIsEnabled": true,
          "messageToEmailAddress": "email To name <email address>",
          "messageFromEmailAddress": "email From name <email address>",
          "messageReplyToEmailAddress": ""
        },

2) Configure an SMTP email server

    The following two sections show the changes in `appsettings.production.json` for configuring an SMTP email server.

    ### SMTP localhost Configuration

        "emailServerSettings": {
          "serverHost": "localhost",
          "serverPort": "25",
          "serverEnableSsl": "false",
          "serverUsername": "",
          "serverPassword": ""
        }

    > Note: For localhost testing using Windows, you can use [Papercut-SMTP](https://www.papercut-smtp.com/).  This is an excellent tool to verify email is working on a production deployment.

    ### Gmail SMTP Configuration
    See [Gmail Help](https://support.google.com/mail/answer/185833) for assistance in creating Gmail application passwords.

        "emailServerSettings": {
          "serverHost": "smtp.gmail.com",
          "serverPort": "587",
          "serverEnableSsl": "true",
          "serverUsername": "{your Gmail email address}",
          "serverPassword": "{your Gmail app password}"
        }

## Execute with Windows Task Scheduler
Using Windows Task Scheduler, create a task and add an Action. Set the `Program/script` using the full path of the application `ddnsupdate.exe` file. Then specify the *Start in* option as the directory path of the `ddnsupdate.exe` application. Finally, set the maximum number of DDNS update iterations to 1 in the `appsettings.production.json` file:

    "ddnsSettings": {
        ...
        "maximumDdnsUpdateIterations": 1
    },

Each time the task is triggered by the Windows Task Scheduler, it will update all DNS records and then exit.  It is up to you to set the number of times the task is executed over time (i.e. Triggers) in the Windows Task Scheduler.

> Note: Setting `maximumDdnsUpdateIterations` to zero allows an unlmited number of iterations.

## License
[Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)

## Author
PaulTechGuy

## Contact
[github@ddnsupdate.net](mailto:github@ddnsupdate.net)
