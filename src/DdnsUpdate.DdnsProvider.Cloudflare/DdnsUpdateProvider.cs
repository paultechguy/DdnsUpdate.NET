// -------------------------------------------------------------------------
// <copyright file="DdnsUpdateProvider.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.DdnsProvider.Cloudflare;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DdnsUpdate.DdnsProvider.Cloudflare.Models;
using DdnsUpdate.DdnsProvider.Interfaces;
using DdnsUpdate.DdnsProvider.Models;
using Microsoft.Extensions.Configuration;

/// <summary>
/// An <see cref="IDdnsUpdateProvider"/> that updates DNS records through the Cloudflare v4 API,
/// using the domains in the <c>cloudflareSettings</c> configuration section.
/// </summary>
/// <remarks>
/// Any empty per-domain value (zone id, record type, authorization key or email) falls back to
/// the value in <c>cloudflareSettings.defaultDomain</c>.
/// </remarks>
public class DdnsUpdateProvider(IConfiguration configuration) : IDdnsUpdateProvider
{
   private readonly IConfiguration configuration = configuration;
   private CloudflareSettings applicationSettings = new();

   /// <inheritdoc/>
   public string ProviderName => "Cloudflare API";

   /// <inheritdoc/>
   /// <remarks>
   /// Re-reads <c>cloudflareSettings</c> so edits made while running are picked up; the other
   /// members use the settings loaded by the most recent call to this method.
   /// </remarks>
   public async Task<List<string>> GetDomainNamesAsync()
   {
      this.RefreshApplicationSettings();

      var enabledDomains = this.applicationSettings.Domains
         .Where(x => x.IsEnabled)
         .Select(x => x.Name)
         .ToList();

      return await Task.FromResult(enabledDomains);
   }

   /// <inheritdoc/>
   public async Task<DdnsProviderSuccessResult> IsDomainValidAsync(string domainName)
   {
      var result = new DdnsProviderSuccessResult
      {
         IsSuccess = false,
         Message = string.Empty,
      };

      CloudflareDomain? domain = this.FindDomain(domainName);
      if (domain is null)
      {
         result.Message = $"Domain {domainName} does not exist";

         return await Task.FromResult(result);
      }

      var errorList = new List<string>();

      // the record id identifies the DNS record itself, so it has no default
      if (string.IsNullOrWhiteSpace(domain.RecordId))
      {
         errorList.Add("recordId is empty");
      }

      // every other value must be set on the domain or on defaultDomain
      if (string.IsNullOrWhiteSpace(this.GetSettingsRecordType(domain)))
      {
         errorList.Add("recordType is empty in both the domain and defaultDomain");
      }

      if (string.IsNullOrWhiteSpace(this.GetSettingsAuthorizationKey(domain)))
      {
         errorList.Add("authorizationKey is empty in both the domain and defaultDomain");
      }

      if (string.IsNullOrWhiteSpace(this.GetSettingsAuthorizationEmail(domain)))
      {
         errorList.Add("authorizationEmail is empty in both the domain and defaultDomain");
      }

      if (string.IsNullOrWhiteSpace(this.GetSettingsZoneId(domain)))
      {
         errorList.Add("zoneId is empty in both the domain and defaultDomain");
      }

      result.IsSuccess = errorList.Count == 0;
      result.Message = result.IsSuccess ? string.Empty : string.Join(", ", errorList);

      return await Task.FromResult(result);
   }

   /// <inheritdoc/>
   public async Task<DdnsProviderSuccessResult> TryUpdateIpAddressAsync(
      HttpClient client,
      string domainName,
      string ipAddress)
   {
      var result = new DdnsProviderSuccessResult
      {
         IsSuccess = false,
         Message = string.Empty,
      };

      CloudflareDomain? domain = this.FindDomain(domainName);
      if (domain is null)
      {
         result.Message = $"Domain {domainName} does not exist";

         return await Task.FromResult(result);
      }

      string error;
      try
      {
         // Global API Key authentication; the caller gives each domain its own client, so
         // setting default headers here does not leak between domains
         client.DefaultRequestHeaders.Clear();
         client.DefaultRequestHeaders.Add("X-Auth-Email", this.GetSettingsAuthorizationEmail(domain));
         client.DefaultRequestHeaders.Add("X-Auth-Key", this.GetSettingsAuthorizationKey(domain));

         string zoneId = this.GetSettingsZoneId(domain);
         string url = $"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records/{domain.RecordId}";

         var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
         {
            type = this.GetSettingsRecordType(domain),
            name = domain.Name,
            content = ipAddress,
         }), Encoding.UTF8, "application/json");

         // Use PATCH, not PUT.  PUT is an overwrite: Cloudflare resets any field we omit
         // back to its default, which silently turns off the proxy (proxied=false) on
         // proxied records.  PATCH changes only the fields we send.
         HttpResponseMessage response = await client.PatchAsync(url, content);
         error = response.IsSuccessStatusCode
               ? string.Empty
               : $"{response.StatusCode}: {response.ReasonPhrase}; {await response.Content.ReadAsStringAsync()}";
      }
      catch (Exception ex)
      {
         error = $"Exception, unable to update IP address for domain {domain.Name}, {ex.Message}";
      }

      return new DdnsProviderSuccessResult
      {
         IsSuccess = string.IsNullOrWhiteSpace(error),
         Message = error,
      };
   }

   private void RefreshApplicationSettings()
   {
      this.applicationSettings = this.configuration.GetSection("cloudflareSettings").Get<CloudflareSettings>() ?? throw new InvalidOperationException();
   }

   private CloudflareDomain? FindDomain(string domainName)
   {
      return this.applicationSettings.Domains
         .FirstOrDefault(x => domainName.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
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
}