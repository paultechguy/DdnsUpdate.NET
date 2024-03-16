// -------------------------------------------------------------------------
// <copyright file="WorkerService.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Service;

using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using DdnsUpdate.Core.Interfaces;
using DdnsUpdate.Core.Models;
using DdnsUpdate.DdnsProvider.Helpers;
using DdnsUpdate.DdnsProvider.Interfaces;
using DdnsUpdate.DdnsProvider.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public partial class WorkerService(
   IConfiguration configuration,
   ILogger<WorkerService> logger,
   IEmailSender emailSender,
   IHttpClientFactory clientFactory,
   CommandLineOptions commandLineOptions) : IWorkerService
{
   private const int OneMinuteMilliseconds = 60000;
   private const string LastIpAddressFileName = "LastIpAddress.txt";
   private const string UriStatisticsFileName = "UriStatistics.json";
   private static readonly object LastIpAddressFileLock = new();
   private static readonly object UriStatisticsFileLock = new();
   private readonly IConfiguration configuration = configuration;
   private readonly ILogger<WorkerService> logger = logger;
   private readonly IHttpClientFactory clientFactory = clientFactory;
   private readonly IEmailSender emailSender = emailSender;
   private readonly CommandLineOptions commandLineOptions = commandLineOptions;
   private readonly Random rndIpAddressProvider = new();
   private readonly Regex ipAddressRegex = EmbeddedIpAddressRegEx();
   private ApplicationSettings appSettings = new();
   private UriStatistics uriStatistics = [];
   private IDdnsUpdateProvider ddnsUpdateProvider;

   public async Task ExecuteAsync(IDdnsUpdateProvider ddnsUpdateProvider, CancellationToken cancelToken)
   {
      // When this method exits, the application will stop.  If this is a service, you typically will
      // stay in a "while" loop until a cancellation is requested.  If this is a schedule task or
      // command-line console application, you may loop a few times, but there is a good chance that
      // the method will exit fairly soon on its own (but you should still check for a cancellation
      // request since the Ctrl-C handler will initiate a cancellation request.

      try
      {
         // set so we don't have to pass this all over
         this.ddnsUpdateProvider = ddnsUpdateProvider;

         // we refresh here mainly so that any initial logging will have the proper values
         this.RefreshApplicationSettings();

         this.LogInitialStartupMessages();

         // load any previous uri stats
         await this.LoadUriStatisticsAsync();

         // just for info sake, log the last known IP address (if it exists)
         this.LogInitialIpAddress();

         // loop forever until a cancellation request is seen
         int loopCounter = 0;
         while (!cancelToken.IsCancellationRequested)
         {
            // do we need to exit based on the number of updates
            if (this.IsMaximumUpdatesReached(loopCounter))
            {
               this.logger.LogInformation($"Maximum DDNS updates ({this.appSettings.DdnsSettings.MaximumDdnsUpdateIterations}) reached; stopping");
               break;
            }

            ++loopCounter;

            // just to make sure we have the latest in case the settings were updated
            // while we were executing
            this.RefreshApplicationSettings();

            List<string> updateDomainNames = await this.ddnsUpdateProvider.GetDomainNamesAsync();
            if (updateDomainNames.Count > 0)
            {
               // get new and last known ip addresses
               (string ip, UriStatisticItem? statsItem) = await this.GetIpAddressV4Async(cancelToken);
               string lastIpAddress = this.LoadLastIpAddress();

               if (string.IsNullOrWhiteSpace(ip) || statsItem is null)
               {
                  this.logger.LogWarning($"#{loopCounter}: Unable to determine external IP address or cancellation requested");
                  _ = this.SleepBetweenAllIpUpdates(cancelToken);

                  continue;
               }
               else if (lastIpAddress == ip && !this.appSettings.DdnsSettings.AlwaysUpdateDdnsEvenIfUnchanged)
               {
                  // ip is he same as last time, and we bypass ddns updates if they are the same
                  this.logger.LogInformation($"#{loopCounter}: IP address {ip} unchanged using {statsItem.Uri}; skip {updateDomainNames.Count} DNS update(s)");
                  _ = this.SleepBetweenAllIpUpdates(cancelToken);

                  continue;
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

               // update all ddns records...woo hoo
               await this.UpdateDomainIpAddresses(updateDomainNames, loopCounter, ip, cancelToken);
            }
            else
            {
               this.logger.LogInformation($"#{loopCounter}: No enabled domains found; skip DNS update(s)");
            }

            // sleep if we're not cancelling
            if (!cancelToken.IsCancellationRequested)
            {
               _ = this.SleepBetweenAllIpUpdates(cancelToken);
            }
         }
      }
      catch (Exception)
      {
         // bubble up
         throw;
      }
      finally
      {
         this.logger.LogDebug($"Ending: {nameof(WorkerService)}.{nameof(this.ExecuteAsync)}");
      }
   }

   private void RefreshApplicationSettings()
   {
      // get the lastest ddns and domain settings and sanity check them; for a hosted service
      // in the background, even using a IOptionsSnapot, you can't DI the IOptions to get the
      // latest.  So we read them manually to keep things refreshed.

      this.appSettings = this.configuration.GetSection("applicationSettings").Get<ApplicationSettings>() ?? throw new InvalidOperationException();
   }

   private async Task UpdateDomainIpAddresses(
      List<string> domainNames,
      int loopCounter,
      string ipAddress,
      CancellationToken cancelToken)
   {
      var parallelOptions = new ParallelOptions
      {
         MaxDegreeOfParallelism = this.appSettings.DdnsSettings.ParallelDdnsUpdateCount < 0
            ? -1 // unlimited
            : this.appSettings.DdnsSettings.ParallelDdnsUpdateCount == 0
               ? domainNames.Count
               : this.appSettings.DdnsSettings.ParallelDdnsUpdateCount,
      };

      Parallel.ForEach(domainNames, async domainName =>
      {
         if (!cancelToken.IsCancellationRequested)
         {
            HttpClient client = this.clientFactory.CreateClient();
            _ = await this.UpdateDomainIpAddressAsync(loopCounter, ipAddress, client, domainName);

            // no need to dispose of client
         }
      });

      // now that all updates are done, update stats
      await this.SaveUriStatisticsAsync();
   }

   private void LogInitialStartupMessages()
   {
      this.logger.LogDebug($"Starting: {nameof(WorkerService)}.{nameof(this.ExecuteAsync)}");

      // how often will we be checking for updates
      decimal updateMilliseconds = this.GetUpdatePauseMilliseconds();
      this.logger.LogInformation($"IP address updates will be performed every {Math.Round(updateMilliseconds / OneMinuteMilliseconds, 2)} minute(s)");

      // will ip address changes generate email notifications
      string notifyUpdates = this.appSettings.WorkerServiceSettings.MessageIsEnabled ? string.Empty : " not";
      this.logger.LogInformation($"IP address updates will{notifyUpdates} push email notifications");

      // put some helpful into in the log (or console) about the data directory
      this.logger.LogInformation($"config files are in {FilePathHelper.ApplicationConfigDirectory}");
      this.logger.LogInformation($"log files are in {FilePathHelper.ApplicationLogDirectory}");
      this.logger.LogInformation($"data files are in {FilePathHelper.ApplicationDataDirectory}");
   }

   private async Task<bool> UpdateDomainIpAddressAsync(
      int counter,
      string ip,
      HttpClient client,
      string domainName)
   {
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

   private async Task<(string IpAddress, UriStatisticItem? StatsItem)> GetIpAddressV4Async(CancellationToken cancelToken)
   {
      // choose an random IP address provider
      int startIndex = this.appSettings.DdnsSettings.RandomizeIpAddressProviderSelecion
         ? this.rndIpAddressProvider.Next(this.uriStatistics.Count)
         : 0;

      using var client = new HttpClient();
      for (int i = startIndex; i < startIndex + this.uriStatistics.Count; ++i)
      {
         try
         {
            if (cancelToken.IsCancellationRequested)
            {
               return (string.Empty, null);
            }

            // what is the true index to use
            int providerIndex = i % this.uriStatistics.Count;
            UriStatisticItem statsItem = this.uriStatistics[providerIndex];

            // get an IP address as a string, and clean it up a bit just in case it's not pretty
            try
            {
               string externalIp = await new HttpClient().GetStringAsync(statsItem.Uri, cancelToken);

               // does this text contain an ip address
               if (!this.TryParseIpAddress(externalIp, out IPAddress? ipAddress))
               {
                  statsItem.IncrementFailCount();

                  continue;
               }

               // increment this uri stats and rewrite stats file
               statsItem.IncrementSuccessCount();

               return (ipAddress!.ToString(), statsItem);
            }
            catch (Exception)
            {
               statsItem.IncrementFailCount();

               continue;
            }
         }
         catch
         {
            // ignore and try again
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

      string foundIp = match.Groups["IpAddress"].Value;
      if (!IPAddress.TryParse(foundIp, out ipAddress))
      {
         // hum...something must be wrong with our regex
         this.logger.LogError($"Internal bug, unable to convert string into IP address: {foundIp}");

         return false;
      }

      return true;
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

      if (!this.appSettings.WorkerServiceSettings.MessageIsEnabled)
      {
         this.logger.LogWarning("Email support disabled.  See appSettings.WorkerServiceSettings.MessageIsEnabled");

         return;
      }

      string appName = FilePathHelper.ApplicationName;

      // if it appears we have email settings, send an email if we pass validation
      if (!string.IsNullOrWhiteSpace(this.appSettings.WorkerServiceSettings.MessageFromEmailAddress)
         && !string.IsNullOrWhiteSpace(this.appSettings.WorkerServiceSettings.MessageToEmailAddress))
      {
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
            await this.emailSender.SendHtmlAsync(
               this.appSettings.WorkerServiceSettings.MessageFromEmailAddress,
               this.appSettings.WorkerServiceSettings.MessageToEmailAddress,
               subject,
               body);

            this.logger.LogInformation("IP address email sent; To: {to}, Subject: {subject}", this.appSettings.WorkerServiceSettings.MessageToEmailAddress, subject);
         }
         catch (Exception ex)
         {
            this.logger.LogError($"IP address email send failed: {ex.Message}");
         }
      }
   }

   private bool SleepBetweenAllIpUpdates(CancellationToken cancelToken)
   {
      int pauseMilliseconds = this.GetUpdatePauseMilliseconds();

      bool wasCanceled = cancelToken.WaitHandle.WaitOne(pauseMilliseconds);

      return wasCanceled;
   }

   private string LoadLastIpAddress()
   {
      string filePath = GetLastIpAddressFilePath();
      if (!File.Exists(filePath))
      {
         // maybe first time we're running; create empty file;
         // this also ensures after the very first time the app runs, we
         // have a "data" directory
         this.SaveLastIpAddress(string.Empty);

         return string.Empty;
      }

      lock (LastIpAddressFileLock)
      {
         return File.ReadAllText(filePath);
      }
   }

   private void SaveLastIpAddress(string ipAddress)
   {
      string filePath = GetLastIpAddressFilePath();
      string folder = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException($"Unable to call {nameof(Path.GetDirectoryName)}");
      if (!Directory.Exists(folder))
      {
         Directory.CreateDirectory(folder);
      }

      lock (LastIpAddressFileLock)
      {
         File.WriteAllText(filePath, ipAddress);
      }
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
      bool maxUpdatesReached = this.appSettings.DdnsSettings.MaximumDdnsUpdateIterations > 0
         && loopCounter >= this.appSettings.DdnsSettings.MaximumDdnsUpdateIterations;

      return maxUpdatesReached;
   }

   private async Task SaveUriStatisticsAsync()
   {
      try
      {
         lock (UriStatisticsFileLock)
         {
            // just Wait inside lock
            this.uriStatistics.WriteFileAsync(GetUriStatisticsFilePath()).Wait();
         }
      }
      catch (Exception ex)
      {
         this.logger.LogWarning($"Unable to save {nameof(UriStatistics)}; will try again next iteration  ({ex.Message})");

         // just continue and hopefully thing will work the next time
         // (maybe an editor has the file locked)
      }

      await Task.CompletedTask;
   }

   private async Task LoadUriStatisticsAsync()
   {
      string filePath = GetUriStatisticsFilePath();
      if (Path.Exists(filePath))
      {
         // load existing uris with their stats
         lock (UriStatisticsFileLock)
         {
            // just Wait inside lock
            this.uriStatistics = UriStatistics.ReadFileAsync(filePath).Result;
         }
      }

      // merge in any uris from the settings
      this.uriStatistics.Merge(this.appSettings.DdnsSettings.IpAddressProviders);

      await Task.CompletedTask;
   }

   private static string GetLastIpAddressFilePath()
   {
      return Path.Combine(FilePathHelper.ApplicationDataDirectory, LastIpAddressFileName);
   }

   private static string GetUriStatisticsFilePath()
   {
      return Path.Combine(FilePathHelper.ApplicationDataDirectory, UriStatisticsFileName);
   }

   private int GetUpdatePauseMilliseconds()
   {
      decimal minutes = this.appSettings.DdnsSettings.AfterAllDdnsUpdatePauseMinutes <= 0M
         ? 1M
         : this.appSettings.DdnsSettings.AfterAllDdnsUpdatePauseMinutes;

      // convert to ms
      return (int)(minutes * OneMinuteMilliseconds);
   }

   [GeneratedRegex(@"(?<IpAddress>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})", RegexOptions.Multiline)]
   private static partial Regex EmbeddedIpAddressRegEx();
}