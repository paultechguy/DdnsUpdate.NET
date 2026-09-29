// -------------------------------------------------------------------------
// <copyright file="WorkerService.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Service;

using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using DdnsUpdate.Core.Helpers;
using DdnsUpdate.Core.Interfaces;
using DdnsUpdate.Core.Models;
using DdnsUpdate.DdnsProvider.Interfaces;
using DdnsUpdate.DdnsProvider.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// The main DDNS update loop: detect the external IP address, and when it has changed,
/// push it to every enabled domain through the <see cref="IDdnsUpdateProvider"/>.
/// </summary>
/// <remarks>
/// State that must survive restarts (the last known IP address and the per-URL IP provider
/// statistics) is kept in <see cref="FilePathHelper.ApplicationDataDirectory"/>.
/// </remarks>
public partial class WorkerService(
   IConfiguration configuration,
   ILogger<WorkerService> logger,
   IEmailSender emailSender,
   IHttpClientFactory clientFactory,
   IDdnsUpdateProvider ddnsUpdateProvider,
   CommandLineOptions commandLineOptions) : IWorkerService
{
   private const string LastIpAddressFileName = "LastIpAddress.txt";
   private const string UriStatisticsFileName = "UriStatistics.json";
   private readonly string lastIPAddressFilePath = GetLastIpAddressFilePath();
   private readonly IConfiguration configuration = configuration;
   private readonly ILogger<WorkerService> logger = logger;
   private readonly IHttpClientFactory clientFactory = clientFactory;
   private readonly IEmailSender emailSender = emailSender;
   private readonly IDdnsUpdateProvider ddnsUpdateProvider = ddnsUpdateProvider;
   private readonly CommandLineOptions commandLineOptions = commandLineOptions;
   private readonly Random rndIpAddressProvider = new();
   private readonly Regex ipAddressRegex = EmbeddedIpAddressRegEx();
   private ApplicationSettings appSettings = new();
   private UriStatistics uriStatistics = [];

   /// <inheritdoc/>
   public async Task ExecuteAsync(CancellationToken cancelToken)
   {
      // When this method returns, the application stops.  As a Windows Service it loops until
      // cancellation is requested; as a Scheduled Task, maximumDdnsUpdateIterations (usually 1)
      // ends the loop instead.  Ctrl-C in console mode also triggers a cancellation.

      try
      {
         // load settings before logging so the startup messages show the configured values
         this.RefreshApplicationSettings();

         this.LogInitialStartupMessages();

         // load any previous uri stats
         await this.LoadUriStatisticsAsync();

         // just for info sake, log the last known IP address (if it exists)
         this.LogInitialIpAddress();

         int loopCounter = 0;
         while (!cancelToken.IsCancellationRequested)
         {
            ++loopCounter;

            // settings files are reloaded on change, so pick up any edits made while running
            this.RefreshApplicationSettings();

            await this.ExecuteIterationAsync(loopCounter, cancelToken);

            // check before sleeping; otherwise a Scheduled Task run would sit through a full
            // pause interval before exiting
            if (this.IsMaximumUpdatesReached(loopCounter))
            {
               this.logger.LogInformation($"Maximum DDNS updates ({this.appSettings.DdnsSettings.MaximumDdnsUpdateIterations}) reached; stopping");
               break;
            }

            this.SleepBetweenAllIpUpdates(cancelToken);
         }
      }
      finally
      {
         this.logger.LogInformation($"Ending: {nameof(WorkerService)}.{nameof(this.ExecuteAsync)}");
      }
   }

   /// <summary>
   /// Performs a single pass: find the enabled domains, get the external IP, and update the
   /// domains if the IP changed (or if updates are forced by configuration).
   /// </summary>
   private async Task ExecuteIterationAsync(int loopCounter, CancellationToken cancelToken)
   {
      List<string> updateDomainNames = await this.ddnsUpdateProvider.GetDomainNamesAsync();
      if (updateDomainNames.Count == 0)
      {
         this.logger.LogInformation($"#{loopCounter}: No enabled domains found; skip DDNS update(s)");
         return;
      }

      // get new and last known ip addresses
      (string ip, UriStatisticItem? statsItem) = await this.GetIpAddressV4Async(cancelToken);
      string lastIpAddress = this.LoadLastIpAddress();

      if (string.IsNullOrWhiteSpace(ip) || statsItem is null)
      {
         this.logger.LogWarning($"#{loopCounter}: Unable to determine external IP address or cancellation requested");
         return;
      }

      if (lastIpAddress == ip && !this.appSettings.DdnsSettings.AlwaysUpdateDdnsEvenIfUnchanged)
      {
         this.logger.LogInformation($"#{loopCounter}: IP address {ip} unchanged using {statsItem.Uri}; skip {updateDomainNames.Count} DDNS update(s)");
         return;
      }

      // remember last ip if it's changed
      if (ip != lastIpAddress)
      {
         this.logger.LogInformation($"New IP address found: {ip}");
         await this.SendEmailIpAddressChangedAsync(lastIpAddress, ip, cancelToken); // optional based on config
         this.SaveLastIpAddress(ip);
      }

      this.logger.LogInformation($"#{loopCounter}: Current external IP is {ip} via URL {statsItem.Uri}");
      this.logger.LogInformation($"#{loopCounter}: Processing IP updates for {updateDomainNames.Count} domain(s)");

      await this.UpdateDomainIpAddressesAsync(updateDomainNames, loopCounter, ip, cancelToken);
   }

   private void RefreshApplicationSettings()
   {
      // IOptions<T> is bound once when the hosted service is created, so it would never see
      // settings edits made while the service runs; read the section directly instead.
      this.appSettings = this.configuration.GetSection("applicationSettings").Get<ApplicationSettings>() ?? throw new InvalidOperationException();
   }

   private async Task UpdateDomainIpAddressesAsync(
      List<string> domainNames,
      int loopCounter,
      string ipAddress,
      CancellationToken cancelToken)
   {
      // parallelDdnsUpdateCount: < 0 = .NET default, 0 = one per domain, > 0 = that many
      var parallelOptions = new ParallelOptions
      {
         CancellationToken = cancelToken,
         MaxDegreeOfParallelism = this.appSettings.DdnsSettings.ParallelDdnsUpdateCount < 0
            ? -1
            : this.appSettings.DdnsSettings.ParallelDdnsUpdateCount == 0
               ? domainNames.Count
               : this.appSettings.DdnsSettings.ParallelDdnsUpdateCount,
      };

      try
      {
         // ForEachAsync (not ForEach) so every update is awaited before the iteration ends
         await Parallel.ForEachAsync(domainNames, parallelOptions, async (domainName, token) =>
         {
            // a client per domain because the provider sets per-domain auth headers on it;
            // the factory owns the handlers, so the client itself needs no disposal
            HttpClient client = this.clientFactory.CreateClient();
            _ = await this.UpdateDomainIpAddressAsync(loopCounter, ipAddress, client, domainName);
         });
      }
      catch (OperationCanceledException)
      {
         // shutting down; any updates not yet started are simply skipped
      }
   }

   private void LogInitialStartupMessages()
   {
      this.logger.LogDebug($"Starting: {nameof(WorkerService)}.{nameof(this.ExecuteAsync)}");

      // how often will we be checking for updates
      decimal updateMinutes = this.appSettings.DdnsSettings.AfterAllDdnsUpdatePauseMinutes;
      this.logger.LogInformation("{updateMinutes}", $"IP address updates will be performed every {updateMinutes} minute(s)");

      // will ip address changes generate email notifications
      string notifyUpdates = this.appSettings.WorkerServiceSettings.MessageIsEnabled ? string.Empty : " not";
      this.logger.LogInformation($"IP address updates will{notifyUpdates} push email notifications");

      // put some helpful into in the log (or console) about the data directory
      this.logger.LogInformation($"Application data files (logs, statistics, etc.) are stored in {FilePathHelper.ApplicationDataDirectory}");
   }

   private async Task<bool> UpdateDomainIpAddressAsync(
      int counter,
      string ip,
      HttpClient client,
      string domainName)
   {
      // catch configuration gaps (e.g. no zone id in the domain or the defaults) with a clear
      // message rather than letting the provider's API reject the request
      DdnsProviderSuccessResult validResult = await this.ddnsUpdateProvider.IsDomainValidAsync(domainName);
      if (!validResult.IsSuccess)
      {
         this.logger.LogError($"#{counter}: Invalid configuration for {domainName}; {validResult.Message}");

         return false;
      }

      DdnsProviderSuccessResult updateResult = await this.ddnsUpdateProvider.TryUpdateIpAddressAsync(
         client,
         domainName,
         ip);
      if (updateResult.IsSuccess)
      {
         this.logger.LogInformation($"#{counter}: Domain {domainName}, IP updated to {ip}");
      }
      else
      {
         this.logger.LogError($"#{counter}: IP update failed for {domainName}, IP {ip}; {updateResult.Message}");
      }

      return updateResult.IsSuccess;
   }

   /// <summary>
   /// Asks the configured IP address providers for the external IPv4 address, trying each in
   /// turn until one returns a parsable address.
   /// </summary>
   /// <returns>The IP address and the provider that supplied it, or an empty string and null
   /// if no provider succeeded or cancellation was requested.</returns>
   private async Task<(string IpAddress, UriStatisticItem? StatsItem)> GetIpAddressV4Async(CancellationToken cancelToken)
   {
      // start at a random provider (by default) to spread the load across the free services,
      // then walk the list circularly so every provider gets one attempt
      int startIndex = this.appSettings.DdnsSettings.RandomizeIpAddressProviderSelecion
         ? this.rndIpAddressProvider.Next(this.uriStatistics.Count)
         : 0;

      HttpClient client = this.clientFactory.CreateClient();
      for (int i = startIndex; i < startIndex + this.uriStatistics.Count; ++i)
      {
         if (cancelToken.IsCancellationRequested)
         {
            return (string.Empty, null);
         }

         UriStatisticItem statsItem = this.uriStatistics[i % this.uriStatistics.Count];

         try
         {
            // providers return plain text, sometimes with extra whitespace or markup
            string externalIp = await client.GetStringAsync(statsItem.Uri, cancelToken);
            if (!this.TryParseIpAddress(externalIp, out IPAddress? ipAddress))
            {
               statsItem.IncrementFailCount();

               continue;
            }

            // the stats file is written on success only; fail counts ride along with the next save
            statsItem.IncrementSuccessCount();
            await this.SaveUriStatisticsAsync();

            return (ipAddress!.ToString(), statsItem);
         }
         catch (OperationCanceledException) when (cancelToken.IsCancellationRequested)
         {
            return (string.Empty, null);
         }
         catch (Exception)
         {
            // unreachable or failing provider; try the next one
            statsItem.IncrementFailCount();
         }
      }

      // if we get here, none of the ip providers worked
      return (string.Empty, null);
   }

   private bool TryParseIpAddress(string value, out IPAddress? ipAddress)
   {
      ipAddress = null;

      // can we parse this text into a valid ip address
      Match match = this.ipAddressRegex.Match(value);
      if (!match.Success)
      {
         return false;
      }

      // the regex accepts octets above 255, so let IPAddress have the final say
      string foundIp = match.Groups["IpAddress"].Value;
      return IPAddress.TryParse(foundIp, out ipAddress);
   }

   private async Task SendEmailIpAddressChangedAsync(
      string oldIpAddress,
      string newIpAddress,
      CancellationToken cancelToken)
   {
      if (cancelToken.IsCancellationRequested)
      {
         return;
      }

      WorkerServiceSettings settings = this.appSettings.WorkerServiceSettings;
      if (!settings.MessageIsEnabled)
      {
         this.logger.LogWarning("Email support disabled.  See appSettings.WorkerServiceSettings.MessageIsEnabled");

         return;
      }

      // To and From are both required; without them there is nothing to send
      if (string.IsNullOrWhiteSpace(settings.MessageFromEmailAddress)
         || string.IsNullOrWhiteSpace(settings.MessageToEmailAddress))
      {
         this.logger.LogWarning("Email support enabled, but messageToEmailAddress or messageFromEmailAddress is empty; no email sent");

         return;
      }

      string appName = FilePathHelper.ApplicationName;
      string oldIp = string.IsNullOrWhiteSpace(oldIpAddress) ? "N/A" : oldIpAddress;
      string subject = $"IP address update from {appName}, {DateTime.Now:G}";
      string body = $$"""
         <html><head></head><body>
         <p><b>Old IP Address Change</b>: <span>{{oldIp}}</span></p>
         <p><b>New IP Address Change</b>: <span>{{newIpAddress}}</span></p>
         <p>/{{appName}}</p>
         </body></html>
         """;

      try
      {
         await this.emailSender.SendAsync(
            settings.MessageFromEmailAddress,
            settings.MessageToEmailAddress,
            subject,
            replyTo: string.IsNullOrWhiteSpace(settings.MessageReplyToEmailAddress) ? null : settings.MessageReplyToEmailAddress,
            bodyHtml: body);

         this.logger.LogInformation("IP address email sent; To: {to}, Subject: {subject}", settings.MessageToEmailAddress, subject);
      }
      catch (Exception ex)
      {
         // email is a courtesy; never let it stop the DNS updates
         this.logger.LogError($"IP address email send failed: {ex.Message}");
      }
   }

   private void SleepBetweenAllIpUpdates(CancellationToken cancelToken)
   {
      const int OneMinuteMs = 60000;
      int pauseMinutes = this.appSettings.DdnsSettings.AfterAllDdnsUpdatePauseMinutes;

      // if missing from config...
      if (pauseMinutes <= 0)
      {
         this.logger.LogWarning($"Configuration property {nameof(this.appSettings.DdnsSettings.AfterAllDdnsUpdatePauseMinutes)} may be missing; defaulting to one minute pause");
         pauseMinutes = 1;
      }

      // returns early if cancellation is requested (e.g. service stop or Ctrl-C)
      _ = cancelToken.WaitHandle.WaitOne(pauseMinutes * OneMinuteMs);
   }

   private string LoadLastIpAddress()
   {
      return File.Exists(this.lastIPAddressFilePath) ? File.ReadAllText(this.lastIPAddressFilePath) : string.Empty;
   }

   private void SaveLastIpAddress(string ipAddress)
   {
      string folder = Path.GetDirectoryName(this.lastIPAddressFilePath) ?? throw new InvalidOperationException(nameof(this.lastIPAddressFilePath));
      if (!Directory.Exists(folder))
      {
         Directory.CreateDirectory(folder);
      }

      File.WriteAllText(this.lastIPAddressFilePath, ipAddress);
   }

   private void LogInitialIpAddress()
   {
      string lastIpAddress = this.LoadLastIpAddress();
      string message = string.IsNullOrWhiteSpace(lastIpAddress)
         ? "none found"
         : lastIpAddress;

      this.logger.LogInformation($"Checking for initial IP address: {message}");
   }

   private bool IsMaximumUpdatesReached(int loopCounter)
   {
      // zero (the default) means run forever
      bool maxUpdatesReached = this.appSettings.DdnsSettings.MaximumDdnsUpdateIterations > 0
         && loopCounter >= this.appSettings.DdnsSettings.MaximumDdnsUpdateIterations;

      return maxUpdatesReached;
   }

   private async Task SaveUriStatisticsAsync()
   {
      try
      {
         await this.uriStatistics.WriteFileAsync(GetUriStatisticsFilePath());
      }
      catch (Exception ex)
      {
         // not critical; the counts stay in memory and the next successful lookup saves them
         // (a common cause is an editor holding the file open)
         this.logger.LogWarning($"Unable to save {nameof(UriStatistics)}; will try again next iteration  ({ex.Message})");
      }
   }

   private async Task LoadUriStatisticsAsync()
   {
      string filePath = GetUriStatisticsFilePath();
      if (Path.Exists(filePath))
      {
         // load existing uris with their stats
         this.uriStatistics = await UriStatistics.ReadFileAsync(filePath);
      }

      // add any providers configured since the stats file was written; providers removed
      // from the settings remain in the file and are still used
      this.uriStatistics.Merge(this.appSettings.DdnsSettings.IpAddressProviders);
   }

   private static string GetLastIpAddressFilePath()
   {
      return Path.Combine(FilePathHelper.ApplicationDataDirectory, LastIpAddressFileName);
   }

   private static string GetUriStatisticsFilePath()
   {
      return Path.Combine(FilePathHelper.ApplicationDataDirectory, UriStatisticsFileName);
   }

   // finds the first dotted-quad in a provider's response; IPAddress.TryParse validates it
   [GeneratedRegex(@"(?<IpAddress>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})", RegexOptions.Multiline)]
   private static partial Regex EmbeddedIpAddressRegEx();
}