// <copyright file="DdnsUpdatePlugin.cs" company="PaulTechGuy"
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>"

namespace DdnsUpdate.DdnsPlugin.Cloudflare;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DdnsUpdate.DdnsPlugin.Cloudflare.Models;
using DdnsUpdate.DdnsPlugin.Interfaces;
using DdnsUpdate.DdnsPlugin.Models;

public class DdnsUpdatePlugin(
   DdnsUpdatePluginInstanceContext contextInstance) : IDdnsUpdatePlugin
{
   private CloudflareSettings applicationSettings = new();
   private HttpClient httpClient = null!;
   private bool disposedValue;
   private readonly DdnsUpdatePluginInstanceContext contextInstance = contextInstance;

   // Interface required
   /// <inheritdoc/>
   public string PluginName { get; set; } = "PaulTechGuy.Cloudflare";

   // Interface required
   /// <inheritdoc/>
   public async Task<List<string>> GetDomainNamesAsync()
   {
      try
      {
         this.RefreshApplicationSettings();

         var enabledDomains = this.applicationSettings.Domains
            .Where(x => x.IsEnabled)
            .Select(x => x.Name)
            .ToList();

         return await Task.FromResult(enabledDomains);
      }
      catch (Exception)
      {
         // hum...not good so we'll just log the issue and pretend we don't have
         // any domains to update
         this.contextInstance.Logger.LogError(this, "Unable to refresh application settings");
         return [];
      }
   }

   // Interface required
   /// <inheritdoc/>
   public async Task BeginBatchUpdateIpAddressAsync()
   {
      this.httpClient = new HttpClient();

      await Task.CompletedTask;
   }

   // Interface required
   /// <inheritdoc/>
   public async Task EndBatchUpdateIpAddressAsync()
   {
      // not actually needed per docs, but doens't hurt
      this.httpClient.Dispose();

      await Task.CompletedTask;
   }

   // Interface required
   /// <inheritdoc/>
   public async Task<DdnsPluginStatusResult> TryUpdateIpAddressAsync(
      string domainName,
      string ipAddress)
   {
      CloudflareDomain? domain = this.applicationSettings.Domains
         .Where(x => domainName.Equals(x.Name, StringComparison.OrdinalIgnoreCase))
         .FirstOrDefault();
      if (domain is null)
      {
         DdnsPluginStatusResult invaliResult = DdnsPluginStatusResult.Fail;
         invaliResult.Message = $"Domain {domainName} does not exist";

         return await Task.FromResult(invaliResult);
      }

      // make sure all the configuration/settings for this domain are valid
      if (!this.IsDomainValidAsync(domain, out DdnsPluginStatusResult result))
      {
         return result;
      }

      string error;
      try
      {
         // we need to set these headers
         this.httpClient.DefaultRequestHeaders.Clear();
         this.httpClient.DefaultRequestHeaders.Add("X-Auth-Email", this.GetSettingsAuthorizationEmail(domain));
         this.httpClient.DefaultRequestHeaders.Add("X-Auth-Key", this.GetSettingsAuthorizationKey(domain));

         string zoneId = this.GetSettingsZoneId(domain);
         string url = $"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records/{domain.RecordId}";

         var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
         {
            type = this.GetSettingsRecordType(domain),
            name = domain.Name,
            content = ipAddress
         }), Encoding.UTF8, "application/json");

         HttpResponseMessage response = await this.httpClient.PutAsync(url, content);
         error = response.IsSuccessStatusCode
               ? string.Empty
               : $"{response.StatusCode}: {response.ReasonPhrase}";
      }
      catch (Exception ex)
      {
         error = $"Exception, unable to update IP address for domain {domain.Name}, {ex.Message}";
      }

      result.IsSuccess = string.IsNullOrWhiteSpace(error);
      result.Message = error;

      return result;
   }

   private bool IsDomainValidAsync(CloudflareDomain domain, out DdnsPluginStatusResult result)
   {
      // make sure we have all the proper settings; if a domain setting is missing, we just have a default instead

      result = DdnsPluginStatusResult.Fail;

      var errorList = new List<string>();

      // if domain has an empty record type, and the default is also empty...we have a problem
      if (string.IsNullOrWhiteSpace(domain.RecordType)
         && string.IsNullOrWhiteSpace(this.applicationSettings.DefaultDomain.RecordType))
      {
         errorList.Add("defaultDomain must have a non-empty recordType");
      }

      // if domain has an empty auth key, and the default is also empty...we have a problem
      if (string.IsNullOrWhiteSpace(domain.AuthorizationKey)
         && string.IsNullOrWhiteSpace(this.applicationSettings.DefaultDomain.AuthorizationKey))
      {
         errorList.Add("defaultDomain must have a non-empty authKey");
      }

      // if domain has an empty auth email, and the default is also empty...we have a problem
      if (string.IsNullOrWhiteSpace(domain.AuthorizationEmail)
         && string.IsNullOrWhiteSpace(this.applicationSettings.DefaultDomain.AuthorizationEmail))
      {
         errorList.Add("defaultDomain must have a non-empty authEmail");
      }

      // if domains has an empty zone id, and the default is also empty...we have a problem
      if (string.IsNullOrWhiteSpace(domain.ZoneId)
         && string.IsNullOrWhiteSpace(this.applicationSettings.DefaultDomain.ZoneId))
      {
         errorList.Add("defaultDomain must have a non-empty zoneId");
      }

      result.IsSuccess = errorList.Count == 0;
      result.Message = result.IsSuccess ? string.Empty : string.Join(", ", errorList);

      return result.IsSuccess;
   }

   private void RefreshApplicationSettings()
   {
      CloudflareSettings? cloudflareJsonSettings = this.contextInstance.Settings.GetSettingsOrNull<CloudflareSettings>(this)
         ?? throw new InvalidOperationException($"Missing application configuraiton for plugin: {this.PluginName}");

      this.applicationSettings = cloudflareJsonSettings;
   }

   private string GetSettingsAuthorizationEmail(CloudflareDomain domain)
   {
      return string.IsNullOrWhiteSpace(domain.AuthorizationEmail) ? this.applicationSettings.DefaultDomain.AuthorizationEmail : domain.AuthorizationEmail;
   }

   private string GetSettingsAuthorizationKey(CloudflareDomain domain)
   {
      return string.IsNullOrWhiteSpace(domain.AuthorizationKey) ? this.applicationSettings.DefaultDomain.AuthorizationKey : domain.AuthorizationKey;
   }

   private string GetSettingsRecordType(CloudflareDomain domain)
   {
      return string.IsNullOrWhiteSpace(domain.RecordType) ? this.applicationSettings.DefaultDomain.RecordType : domain.RecordType;
   }

   private string GetSettingsZoneId(CloudflareDomain domain)
   {
      return string.IsNullOrWhiteSpace(domain.ZoneId) ? this.applicationSettings.DefaultDomain.ZoneId : domain.ZoneId;
   }

   protected virtual void Dispose(bool disposing)
   {
      if (!this.disposedValue)
      {
         if (disposing)
         {
            // dispose managed state (managed objects)
            this.httpClient?.Dispose();
         }

         // free unmanaged resources (unmanaged objects) and override finalizer
         // set large fields to null
         this.disposedValue = true;
      }
   }

   // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
   // ~DdnsUpdatePlugin()
   // {
   //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
   //     Dispose(disposing: false);
   // }

   public void Dispose()
   {
      // Dd not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      this.Dispose(disposing: true);
      GC.SuppressFinalize(this);
   }
}
