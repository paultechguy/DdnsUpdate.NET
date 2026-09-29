# DdnsUpdate.NET

The primary use case for this application is to update Cloudflare DNS records with an externally visible IP address.  It allows you to continuously update your Cloudflare DNS records using the Cloudflare dynamic DNS (DDNS) API.  The application can be used as a Windows Service, Scheduled Task, or an interactive Console Application.  The C# source code is also included if needed.

This project is based off the .NET Windows Service template at [paultechguy/WinService.Net](https://github.com/paultechguy/WinService.Net).

## Terminology
For this document, the use of the word *application* refers to the DdnsUpdate.NET application.

## Requirements
- Cloudflare account with one or more domains
- Windows 10 or higher, win-x64
- .NET 10 SDK, e.g. with Visual Studio 2026 (only if you want to build from source; see [docs/BUILDING.md](docs/BUILDING.md))

## Take A Test Drive
Before you configure the application to update your Cloudflare DNS records with your external IP address, you should test the application to make sure it works with the default configuration.  Doing this will ensure the configuration is correct and features such as logging are working.

The application directory should have the following files:

    appsettings.json
    appsettings.production.json
    DdnsUpdate.exe
    readme.url

To test the application, open up a Windows console and execute the application:

    .\DdnsUpdate.exe

If everything is working correctly, you should see:

    [12:30:54 INF] PRODUCTION environment detected
    [12:30:54 INF] Starting DdnsUpdate by PaulTechGuy, v0.1.0.0
    [12:30:54 INF] Press Ctrl-C to cancel
    [12:30:54 INF] IP address updates will be performed every 60 minute(s)
    [12:30:54 INF] IP address updates will not push email notifications
    [12:30:54 INF] Application data files (logs, statistics, etc.) are stored in C:\ProgramData\PaulTechGuy\DdnsUpdate
    [12:30:54 INF] Checking for initial IP address: none found
    [12:30:54 INF] #1: No enabled domains found; skip DDNS update(s)

## Configuration
Once you have confirmed the application executes successfully out-of-the-box, you can configure it to update your Cloudflare domains. This is done by editing the `domains` JSON property in the `cloudflareSettings` section.  By default, this property contains an example DNS update configuration that is disabled.

### Where to put your settings
Settings are read from these files in the application directory, in order; a value in a later file overrides the same value in an earlier one:

1. `appsettings.json`
1. `appsettings.production.json`
1. `appsettings.production.user.json` (optional; you create it)

We recommend putting your own values, especially the Cloudflare key and any SMTP password, in `appsettings.production.user.json`.  It only needs the properties you want to override, and because it is not part of the release, installing a new version of the application never overwrites it.  The examples below show the JSON sections to add; place them inside the file's outer `{ }`.

Settings are re-read on every update pass, so most changes take effect without restarting the application.  The email server settings (`emailSmtpSettings`) are the exception and require a restart.

### Gathering your Cloudflare values
To add DNS configurations, you will need to gather some information from your Cloudflare account.  For each domain, you will need the `Domain name`, `zone ID`, and the `record ID`.  In addition, for authenticating with the Cloudflare API, you will need your `authorization email` and `authorization key`.  Using your Cloudflare account, you can obtain these values:

1.  Domain Name: This is the DNS record name to update (e.g. mycompany.com or home.mycompany.com).
1.  Authorization Email: This is the email used for your account.
1.  Authorization Key: Navigate to the *API Tokens* tab and view your *Global API Key*.
1.  Zone ID: Navigate to the domain name.  On the *Overview* tab, you can view the Zone ID in the lower-right panel.
1.  Record ID: This can be a bit difficult to find.  There are a few methods.
    1. Navigate to the *Manage Account* tab and view the Audit Log. If you have recently edited a domain name in your account, you can view that specific log entry.  It should contain a reference to the record ID.
    1. You can use the Cloudflare API to get the record ID.  Using a *curl* command or a tool such as [Postman](https://www.postman.com/), submit a GET request.  You should be able to obtain the record ID from the result. Here is the format of an example *curl* command (replace the \{...\} tags with the required values):

          curl --location 'https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records' \
            --header 'Content-Type: application/json' \
            --header 'X-Auth-Email: {authEmail}' \
            --header 'X-Auth-Key: {authKey}'

### Configuring your domains
Using these five values, you can now configure your DNS configuration(s):

    "cloudflareSettings": {
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
    }

If you have more than one DNS record to configure, you can also enter default values in the `defaultDomain` property, and leave them blank in `domains`:

    "cloudflareSettings": {
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
    }

>In this example, we opted to enter the zone ID in the domain property rather than the defaults. This is because zone IDs tend to be specific for different domains.

If a required value is missing from both the domain and `defaultDomain`, the application logs an `Invalid configuration` error naming the missing values and skips that domain.

The application updates records with a PATCH request, so other record settings, such as whether the record is proxied by Cloudflare, are left unchanged.

### Testing your configuration
You can now test the application to determine if the configuration values are correct, and if your Cloudflare DNS records are updating correctly.  Executing the application in a Windows console should indicate your domain(s) are updated:

    [12:30:54 INF] PRODUCTION environment detected
    [12:30:54 INF] Starting DdnsUpdate by PaulTechGuy, v0.1.0.0
    [12:30:54 INF] Press Ctrl-C to cancel
    [12:30:54 INF] IP address updates will be performed every 60 minute(s)
    [12:30:54 INF] IP address updates will not push email notifications
    [12:30:54 INF] Application data files (logs, statistics, etc.) are stored in C:\ProgramData\PaulTechGuy\DdnsUpdate
    [12:35:51 INF] Checking for initial IP address: none found
    [12:35:52 INF] New IP address found: 196.29.73.21
    [12:35:52 WRN] Email support disabled.  See appSettings.WorkerServiceSettings.MessageIsEnabled
    [12:35:52 INF] #1: Current external IP is 196.29.73.21 via URL https://wtfismyip.com/text
    [12:35:52 INF] #1: Processing IP updates for 1 domain(s)
    [12:35:52 INF] #1: Domain mycompany.com, IP updated to 196.29.73.21

The application only calls Cloudflare when the external IP address differs from the last one it saw, so a second run will report `IP address ... unchanged` and skip the update.  To force an update while testing, temporarily set `alwaysUpdateDdnsEvenIfUnchanged` to `true`:

    "applicationSettings": {
        "ddnsSettings": {
            "alwaysUpdateDdnsEvenIfUnchanged": true
        }
    }

>You can view log files, the last known external IP address, and a statistics file in the path referenced in the above output:

     C:\ProgramData\PaulTechGuy\DdnsUpdate

     logs (directory)
     LastIpAddress.txt
     UriStatistics.json


## Execute as a Windows Service
The following commands can be executed in an administrator-mode Windows console.  For some of the steps, you can also use the standard Windows Services UI (e.g., start, stop).

1) Create the service

        sc.exe create "Cloudflare DDNS Update" binpath="C:\...\{yourFullBinPath}\DdnsUpdate.exe"

2) Start the service

        sc.exe start "Cloudflare DDNS Update"

    By default, the *Startup Type* for the service is Manual.  To automatically start the application when Windows starts, use the Windows Services UI to update the *Startup Type*.

3) Stop the service

        sc.exe stop "Cloudflare DDNS Update"

4) Delete the service

        sc.exe delete "Cloudflare DDNS Update"

If deleting a service fails, ensure you have closed the Windows Services window before performing the delete.

>If you want to verify your service is running, you can check the log file for messages (see Log Files).

## DNS Update Interval
The default functionality is for the application to loop every sixty (60) minutes and update DNS records with the most recent external IP address. You can change this behavior by modifying the number of minutes to pause after each DDNS update:

    "applicationSettings": {
        "ddnsSettings": {
            "afterAllDdnsUpdatePauseMinutes": 10
        }
    }

## Log Files
Log and other application files are stored in `%ProgramData%\PaulTechGuy\DdnsUpdate`. The subdirectory for log files is called `logs`.  The most recent 31 days of log files are saved.

## Email Notifications
By default, the application will not send an email when the external IP address changes. If you want to be notified when the external IP address changes, you can enable this in your settings.

To enable email, you will need access to an external SMTP email server.  Once an SMTP email server is available, complete the following configuration steps:

1) Enable email and set the addresses:

        "applicationSettings": {
            "workerServiceSettings": {
                "messageIsEnabled": true,
                "messageToEmailAddress": "email To name <email address>",
                "messageFromEmailAddress": "email From name <email address>",
                "messageReplyToEmailAddress": ""
            }
        }

2) Configure an SMTP email server

    For localhost testing you can use [Papercut-SMTP](https://www.papercut-smtp.com/).  This is an excellent tool to verify email is working before a production deployment.

    The following two sections show the settings for configuring an SMTP email server.  Like `workerServiceSettings`, `emailSmtpSettings` goes inside `applicationSettings`.

    ### Papercut-SMTP Configuration

        "emailSmtpSettings": {
            "smtpHost": "localhost",
            "smtpPort": 25,
            "smtpEnableSsl": false,
            "smtpUsername": "",
            "smtpPassword": ""
        }

    ### Gmail SMTP Configuration

        "emailSmtpSettings": {
            "smtpHost": "smtp.gmail.com",
            "smtpPort": 587,
            "smtpEnableSsl": true,
            "smtpUsername": "{your Gmail email address}",
            "smtpPassword": "{your Gmail app password}"
        }

    >See [Gmail Help](https://support.google.com/mail/answer/185833) for assistance in creating Gmail application passwords.

## Execute with Windows Task Scheduler
Using Windows Task Scheduler, create a task and add an Action. Set the `Program/script` using the full path of the application `DdnsUpdate.exe` file. Then specify the *Start in* option as the directory path of the `DdnsUpdate.exe` application. Finally, set the maximum number of DDNS update iterations to 1:

    "applicationSettings": {
        "ddnsSettings": {
            "maximumDdnsUpdateIterations": 1
        }
    }

Each time the task is triggered by the Windows Task Scheduler, it will check the external IP address, update all DNS records if it has changed, and then exit.  It is up to you to set the number of times the task is executed over a period of time (i.e. Triggers) in the Windows Task Scheduler.

>The application always exits with code 1 so that Windows Service recovery options work, so Task Scheduler will show a *Last Run Result* of `0x1` even for a successful run.  Check the log file to confirm the outcome.

## License
[MIT](LICENSE.txt)

## Author
PaulTechGuy
