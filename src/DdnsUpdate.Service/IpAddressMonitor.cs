// -------------------------------------------------------------------------
// <copyright file="IpAddressMonitorService.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Service;

using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using DdnsUpdate.Core.Extensions;
using DdnsUpdate.Core.Interfaces;
using DdnsUpdate.Core.Models;
using DdnsUpdate.DdnsProvider;
using DdnsUpdate.DdnsProvider.Helpers;
using DdnsUpdate.DdnsProvider.Interfaces;
using DdnsUpdate.DdnsProvider.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public partial class IpAddressMonitor(
   IConfiguration configuration,
   IPluginManager pluginManager,
   ILogger<IpAddressMonitor> logger,
   IEmailSender emailSender,
   IHttpClientFactory clientFactory,
   CommandLineOptions commandLineOptions) : IIpAddressMonitorService
{
   private const int OneMinuteMilliseconds = 60000;
   private const string LastIpAddressFileName = "LastIpAddress.txt";
   private const string UriStatisticsFileName = "UriStatistics.json";
   private static readonly object LastIpAddressFileLock = new();
   private static readonly object UriStatisticsFileLock = new();
   private readonly IConfiguration configuration = configuration;
   private readonly IPluginManager pluginManager = pluginManager;
   private readonly ILogger<IpAddressMonitor> logger = logger;
   private readonly IHttpClientFactory clientFactory = clientFactory;
   private readonly IEmailSender emailSender = emailSender;
   private readonly CommandLineOptions commandLineOptions = commandLineOptions;
   private readonly Random rndIpAddressProvider = new();
   private readonly Regex ipAddressRegex = EmbeddedIpAddressRegEx();
   private ApplicationSettings appSettings = new();
   private UriStatistics uriStatistics = [];

   public async Task ExecuteAsync(CancellationToken cancelToken)
   {
      // When this method exits, the application will stop.  If this is a service, you typically will
      // stay in a "while" loop until a cancellation is requested.  If this is a schedule task or
      // command-line console application, you may loop a few times, but there is a good chance that
      // the method will exit fairly soon on its own (but you should still check for a cancellation
      // request since the Ctrl-C handler will initiate a cancellation request.

      IList<IDdnsUpdateProvider> providerPlugins = [];
      try
      {
         // we refresh here mainly so that any initial logging will have the proper values
         this.RefreshApplicationSettings();

         this.LogInitialStartupMessages();

         // load any previous uri stats
         await this.LoadUriStatisticsAsync();

         // just for info sake, log the last known IP address (if it exists)
         this.LogInitialIpAddress();

         // load all plugins
         providerPlugins = this.LoadDdnsUpdateProviderPlugins();

         // loop forever until a cancellation request is seen
         int loopCounter = 0;
         while (!cancelToken.IsCancellationRequested)
         {
            // do we need to exit based on the number of updates
            if (this.IsMaximumUpdatesReached(loopCounter))
            {
               this.logger.LogAny(LogLevel.Information, $"Maximum DDNS updates ({this.appSettings.DdnsSettings.MaximumDdnsUpdateIterations}) reached; stopping");
               break;
            }

            ++loopCounter;

            // just to make sure we have the latest in case the settings were updated
            // while we were executing
            this.RefreshApplicationSettings();

            // get new and last known ip addresses
            (string ip, UriStatisticItem? statsItem) = await this.GetIpAddressV4Async(cancelToken);
            string lastIpAddress = this.LoadLastIpAddress();

            if (string.IsNullOrWhiteSpace(ip) || statsItem is null)
            {
               this.logger.LogAny(LogLevel.Warning, $"#{loopCounter}: Unable to determine external IP address or cancellation requested");
               _ = this.SleepBetweenAllIpUpdates(cancelToken);

               continue;
            }
            else if (lastIpAddress == ip && !this.appSettings.DdnsSettings.AlwaysUpdateDdnsEvenIfUnchanged)
            {
               // ip is he same as last time, and we bypass ddns updates if they are the same
               this.logger.LogAny(LogLevel.Information, $"#{loopCounter}: IP address {ip} unchanged using {statsItem.Uri}; skip DNS update(s)");
               _ = this.SleepBetweenAllIpUpdates(cancelToken);

               continue;
            }

            ////////////////////////////
            // START: MAIN PLUGIN LOOP
            //
            foreach (IDdnsUpdateProvider ddnsUpdateProvider in providerPlugins)
            {
               List<string> updateDomainNames = await ddnsUpdateProvider.GetDomainNamesAsync();
               if (updateDomainNames.Count > 0)
               {
                  this.logger.LogAny(LogLevel.Information, $"#{loopCounter}: Current external IP is {ip} via URL {statsItem.Uri}");
                  this.logger.LogAny(LogLevel.Information, $"#{loopCounter}: Processing IP updates for {updateDomainNames.Count} domain(s)");

                  // update all ddns records...woo hoo
                  await this.UpdateDomainIpAddresses(ddnsUpdateProvider, updateDomainNames, loopCounter, ip, cancelToken);
               }
               else
               {
                  this.logger.LogAny(LogLevel.Information, $"#{loopCounter}: No enabled domains found; skip DNS update(s)");
               }
            }

            //
            // END: MAIN PLUGIN LOOP
            ////////////////////////////

            // remember last ip if it's changed
            if (ip != lastIpAddress)
            {
               this.logger.LogAny(LogLevel.Information, $"New IP address found: {ip}");
               await this.SendEmailIpAddressChangedAsync(lastIpAddress, ip, cancelToken); // optional based on config
               this.SaveLastIpAddress(ip);
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
         this.logger.LogAny(LogLevel.Debug, $"Ending: {nameof(IpAddressMonitor)}.{nameof(this.ExecuteAsync)}");

         // now that we've signaled we're done, dispose of plugins; do it after everything in case it bombs, we're basically clean
         if (providerPlugins != null)
         {
            UnloadDdnsUpdateProviderPlugins(providerPlugins);
         }
      }
   }

   private IList<IDdnsUpdateProvider> LoadDdnsUpdateProviderPlugins()
   {
      // we'll need a context instance when providers are created (ctor call)
      var loggerContext = new LoggerContext(
         this.PluginLogInformation,
         this.PluginLogWarning,
         this.PluginLogError,
         this.PluginLogCritical);

      var contextInstance = new DdnsUpdateProviderInstanceContext(this.configuration, loggerContext);

      // load all plugins from main plugin directory
      string topLevelPluginDirectory = FilePathHelper.ApplicationPluginDirectory;
      this.pluginManager.AddProviders(contextInstance, topLevelPluginDirectory, recursive: true);

      return this.pluginManager.Providers;
   }

   private static void UnloadDdnsUpdateProviderPlugins(IList<IDdnsUpdateProvider> providerPlugins)
   {
      // basically, dispose of plugins
      foreach (IDdnsUpdateProvider plugin in providerPlugins)
      {
         plugin.Dispose();
      }
   }

   private void PluginLogInformation(string pluginLogName, string message)
   {
      this.logger.LogAny(LogLevel.Information, message, pluginLogName);
   }

   private void PluginLogWarning(string pluginLogName, string message)
   {
      this.logger.LogAny(LogLevel.Warning, message, pluginLogName);
   }

   private void PluginLogError(string pluginLogName, string message)
   {
      this.logger.LogAny(LogLevel.Error, message, pluginLogName);
   }

   private void PluginLogCritical(string pluginLogName, string message)
   {
      this.logger.LogAny(LogLevel.Critical, message, pluginLogName);
   }

   private void RefreshApplicationSettings()
   {
      // get the lastest ddns and domain settings and sanity check them; for a hosted service
      // in the background, even using a IOptionsSnapot, you can't DI the IOptions to get the
      // latest.  So we read them manually to keep things refreshed.

      this.appSettings = this.configuration.GetSection("applicationSettings").Get<ApplicationSettings>() ?? throw new InvalidOperationException();
   }

   private async Task UpdateDomainIpAddresses(
      IDdnsUpdateProvider ddnsUpdateProvider,
      List<string> domainNames,
      int loopCounter,
      string ipAddress,
      CancellationToken cancelToken)
   {
      int maxDegreeOfParallelism = this.appSettings.DdnsSettings.ParallelDdnsUpdateCount < 0
            ? -1 // unlimited
            : this.appSettings.DdnsSettings.ParallelDdnsUpdateCount == 0
               ? domainNames.Count
               : this.appSettings.DdnsSettings.ParallelDdnsUpdateCount;

      var parallelOptions = new ParallelOptions
      {
         MaxDegreeOfParallelism = maxDegreeOfParallelism,
      };

      // ensure the foreach is completed before continuing
      var parallelForEachTask = Task.Run(() =>
         {
            Parallel.ForEach(domainNames, parallelOptions, async domainName =>
            {
               if (!cancelToken.IsCancellationRequested)
               {
                  _ = await this.UpdateDomainIpAddressAsync(ddnsUpdateProvider, loopCounter, ipAddress, domainName);
               }
            });
         }, cancelToken);

      await parallelForEachTask;

      // now that all updates are done, update stats
      await this.SaveUriStatisticsAsync();
   }

   private void LogInitialStartupMessages()
   {
      this.logger.LogAny(LogLevel.Debug, $"Starting: {nameof(IpAddressMonitor)}.{nameof(this.ExecuteAsync)}");

      // how often will we be checking for updates
      decimal updateMilliseconds = this.GetUpdatePauseMilliseconds();
      this.logger.LogAny(LogLevel.Information, $"IP address updates will be performed every {Math.Round(updateMilliseconds / OneMinuteMilliseconds, 2)} minute(s)");

      // will ip address changes generate email notifications
      string notifyUpdates = this.appSettings.WorkerServiceSettings.MessageIsEnabled ? string.Empty : " not";
      this.logger.LogAny(LogLevel.Information, $"IP address updates will{notifyUpdates} push email notifications");

      // put some helpful into in the log (or console) about the data directory
      this.logger.LogAny(LogLevel.Information, $"config files are in {FilePathHelper.ApplicationConfigDirectory}");
      this.logger.LogAny(LogLevel.Information, $"log files are in {FilePathHelper.ApplicationLogDirectory}");
      this.logger.LogAny(LogLevel.Information, $"data files are in {FilePathHelper.ApplicationDataDirectory}");
   }

   private async Task<bool> UpdateDomainIpAddressAsync(
      IDdnsUpdateProvider ddnsUpdateProvider,
      int counter,
      string ip,
      string domainName)
   {
      DdnsProviderSuccessResult updateResult = await ddnsUpdateProvider.TryUpdateIpAddressAsync(
         domainName,
         ip);
      if (updateResult.IsSuccess)
      {
         this.logger.LogAny(LogLevel.Information, $"#{counter}: Domain {domainName}, IP updated to {ip}", ddnsUpdateProvider.ProviderLogName);
      }
      else
      {
         this.logger.LogAny(LogLevel.Error, $"#{counter}: IP update failed for {domainName}, IP {ip}; {updateResult.Message}");
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
         this.logger.LogAny(LogLevel.Error, $"Internal bug, unable to convert string into IP address: {foundIp}");

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
         this.logger.LogAny(LogLevel.Warning, "Email support disabled.  See appSettings.WorkerServiceSettings.MessageIsEnabled");

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

            this.logger.LogAny(LogLevel.Information, $"IP address email sent; To: {this.appSettings.WorkerServiceSettings.MessageToEmailAddress}, Subject: {subject}");
         }
         catch (Exception ex)
         {
            this.logger.LogAny(LogLevel.Error, $"IP address email send failed: {ex.Message}");
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

      this.logger.LogAny(LogLevel.Information, $"Checking for initial IP address: {message}");
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
         this.logger.LogAny(LogLevel.Warning, $"Unable to save {nameof(UriStatistics)}; will try again next iteration  ({ex.Message})");

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