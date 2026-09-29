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
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DdnsUpdate.DdnsProvider.Cloudflare.Models;
using DdnsUpdate.DdnsProvider.Interfaces;
using DdnsUpdate.DdnsProvider.Models;
using Microsoft.Extensions.Options;

/// <summary>
/// An <see cref="IDdnsUpdateProvider"/> that updates DNS records through the Cloudflare v4 API,
/// using the domains in the <c>cloudflareSettings</c> configuration section.
/// </summary>
/// <remarks>
/// Any empty per-domain value (zone id, record type, credentials) falls back to the value in
/// <c>cloudflareSettings.defaultDomain</c>. For credentials, a domain's own API token or Global
/// API Key wins over the defaults, and at either level an API token wins over a key.
/// Register with <see cref="CloudflareServiceCollectionExtensions.AddCloudflareDdnsProvider"/>.
/// </remarks>
public sealed class DdnsUpdateProvider(
   IOptionsMonitor<CloudflareSettings> settingsMonitor,
   IHttpClientFactory clientFactory)
   : IDdnsUpdateProvider
{
   /// <summary>
   /// The name of the <see cref="HttpClient"/> configured for the Cloudflare API.
   /// </summary>
   public const string HttpClientName = "Cloudflare";

   private readonly IOptionsMonitor<CloudflareSettings> settingsMonitor = settingsMonitor;
   private readonly IHttpClientFactory clientFactory = clientFactory;
   private CloudflareSettings applicationSettings = settingsMonitor.CurrentValue;

   /// <inheritdoc/>
   public string ProviderName => "Cloudflare API";

   /// <inheritdoc/>
   /// <remarks>
   /// Takes a fresh snapshot of <c>cloudflareSettings</c> so edits made while running are picked
   /// up; the other members use that snapshot, keeping a whole pass on consistent settings.
   /// </remarks>
   public Task<List<string>> GetDomainNamesAsync()
   {
      this.applicationSettings = this.settingsMonitor.CurrentValue;

      List<string> enabledDomains = this.applicationSettings.Domains
         .Where(x => x.IsEnabled)
         .Select(x => x.Name)
         .ToList();

      return Task.FromResult(enabledDomains);
   }

   /// <inheritdoc/>
   public Task<DdnsProviderSuccessResult> IsDomainValidAsync(string domainName)
   {
      CloudflareDomain? domain = this.FindDomain(domainName);
      if (domain is null)
      {
         return Task.FromResult(Failure($"Domain {domainName} does not exist"));
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

      // either an API token, or a Global API Key plus its account email
      Credentials credentials = this.GetSettingsCredentials(domain);
      if (string.IsNullOrWhiteSpace(credentials.ApiToken))
      {
         if (string.IsNullOrWhiteSpace(credentials.AuthorizationKey))
         {
            errorList.Add("apiToken and authorizationKey are empty in both the domain and defaultDomain");
         }
         else if (string.IsNullOrWhiteSpace(credentials.AuthorizationEmail))
         {
            errorList.Add("authorizationEmail is empty in both the domain and defaultDomain");
         }
      }

      if (string.IsNullOrWhiteSpace(this.GetSettingsZoneId(domain)))
      {
         errorList.Add("zoneId is empty in both the domain and defaultDomain");
      }

      return Task.FromResult(errorList.Count == 0
         ? new DdnsProviderSuccessResult { IsSuccess = true }
         : Failure(string.Join(", ", errorList)));
   }

   /// <inheritdoc/>
   public async Task<DdnsProviderSuccessResult> TryUpdateIpAddressAsync(
      string domainName,
      string ipAddress,
      CancellationToken cancelToken = default)
   {
      CloudflareDomain? domain = this.FindDomain(domainName);
      if (domain is null)
      {
         return Failure($"Domain {domainName} does not exist");
      }

      try
      {
         using HttpRequestMessage request = this.CreateRecordRequest(HttpMethod.Patch, domain);

         // Use PATCH, not PUT.  PUT is an overwrite: Cloudflare resets any field we omit
         // back to its default, which silently turns off the proxy (proxied=false) on
         // proxied records.  PATCH changes only the fields we send.
         request.Content = JsonContent.Create(new
         {
            type = this.GetSettingsRecordType(domain),
            name = domain.Name,
            content = ipAddress,
         });

         HttpClient client = this.clientFactory.CreateClient(HttpClientName);
         using HttpResponseMessage response = await client.SendAsync(request, cancelToken);
         if (!response.IsSuccessStatusCode)
         {
            string body = (await response.Content.ReadAsStringAsync(cancelToken)).Trim();
            return Failure($"{response.StatusCode}: {response.ReasonPhrase}; {body}");
         }

         return new DdnsProviderSuccessResult { IsSuccess = true };
      }
      catch (Exception ex)
      {
         return Failure($"Exception, unable to update IP address for domain {domain.Name}, {ex.Message}");
      }
   }

   /// <inheritdoc/>
   /// <remarks>
   /// Sends a GET for the configured record. Besides proving the credentials, zone id and
   /// record id work, it fails if the record id belongs to a different name or record type
   /// than configured, which would otherwise make an update overwrite the wrong record.
   /// </remarks>
   public async Task<DdnsProviderRecordResult> GetDnsRecordAsync(
      string domainName,
      CancellationToken cancelToken = default)
   {
      CloudflareDomain? domain = this.FindDomain(domainName);
      if (domain is null)
      {
         return RecordFailure($"Domain {domainName} does not exist");
      }

      try
      {
         using HttpRequestMessage request = this.CreateRecordRequest(HttpMethod.Get, domain);
         HttpClient client = this.clientFactory.CreateClient(HttpClientName);
         using HttpResponseMessage response = await client.SendAsync(request, cancelToken);
         string body = (await response.Content.ReadAsStringAsync(cancelToken)).Trim();
         if (!response.IsSuccessStatusCode)
         {
            return RecordFailure($"{response.StatusCode}: {response.ReasonPhrase}; {body}");
         }

         // { "success": true, "result": { "name": "...", "type": "A", "content": "1.2.3.4", ... } }
         using JsonDocument json = JsonDocument.Parse(body);
         JsonElement record = json.RootElement.GetProperty("result");
         string name = record.GetProperty("name").GetString() ?? string.Empty;
         string type = record.GetProperty("type").GetString() ?? string.Empty;
         string content = record.GetProperty("content").GetString() ?? string.Empty;

         if (!name.Equals(domain.Name, StringComparison.OrdinalIgnoreCase))
         {
            return RecordFailure($"recordId {domain.RecordId} is the record for {name}, not {domain.Name}");
         }

         string recordType = this.GetSettingsRecordType(domain);
         if (!type.Equals(recordType, StringComparison.OrdinalIgnoreCase))
         {
            return RecordFailure($"the record is type {type}, but recordType is {recordType}");
         }

         return new DdnsProviderRecordResult { IsSuccess = true, CurrentIpAddress = content };
      }
      catch (Exception ex) when (ex is not OperationCanceledException || !cancelToken.IsCancellationRequested)
      {
         return RecordFailure($"Exception, unable to read the DNS record for domain {domain.Name}, {ex.Message}");
      }
   }

   private static DdnsProviderSuccessResult Failure(string message)
   {
      return new DdnsProviderSuccessResult { IsSuccess = false, Message = message };
   }

   private static DdnsProviderRecordResult RecordFailure(string message)
   {
      return new DdnsProviderRecordResult { IsSuccess = false, Message = message };
   }

   /// <summary>
   /// Creates a request for the domain's DNS record, with its credentials, relative to the
   /// client's base address (see AddCloudflareDdnsProvider).
   /// </summary>
   private HttpRequestMessage CreateRecordRequest(HttpMethod method, CloudflareDomain domain)
   {
      string zoneId = this.GetSettingsZoneId(domain);
      var request = new HttpRequestMessage(method, $"zones/{zoneId}/dns_records/{domain.RecordId}");

      // headers go on the request, not the client, because domains are updated in parallel
      // and may use different credentials
      Credentials credentials = this.GetSettingsCredentials(domain);
      if (!string.IsNullOrWhiteSpace(credentials.ApiToken))
      {
         request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.ApiToken);
      }
      else
      {
         request.Headers.Add("X-Auth-Email", credentials.AuthorizationEmail);
         request.Headers.Add("X-Auth-Key", credentials.AuthorizationKey);
      }

      return request;
   }

   private CloudflareDomain? FindDomain(string domainName)
   {
      return this.applicationSettings.Domains
         .FirstOrDefault(x => domainName.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
   }

   private Credentials GetSettingsCredentials(CloudflareDomain domain)
   {
      CloudflareDefaultDomain defaults = this.applicationSettings.DefaultDomain;
      string email = string.IsNullOrWhiteSpace(domain.AuthorizationEmail) ? defaults.AuthorizationEmail : domain.AuthorizationEmail;

      // a domain's own credentials beat the defaults, so one domain can use a different
      // account; at each level a (scoped) API token beats the (all-powerful) Global API Key
      if (!string.IsNullOrWhiteSpace(domain.ApiToken))
      {
         return new Credentials(domain.ApiToken, string.Empty, string.Empty);
      }

      if (!string.IsNullOrWhiteSpace(domain.AuthorizationKey))
      {
         return new Credentials(string.Empty, domain.AuthorizationKey, email);
      }

      if (!string.IsNullOrWhiteSpace(defaults.ApiToken))
      {
         return new Credentials(defaults.ApiToken, string.Empty, string.Empty);
      }

      return new Credentials(string.Empty, defaults.AuthorizationKey, email);
   }

   private string GetSettingsRecordType(CloudflareDomain domain)
   {
      return string.IsNullOrWhiteSpace(domain.RecordType) ? this.applicationSettings.DefaultDomain.RecordType : domain.RecordType;
   }

   private string GetSettingsZoneId(CloudflareDomain domain)
   {
      return string.IsNullOrWhiteSpace(domain.ZoneId) ? this.applicationSettings.DefaultDomain.ZoneId : domain.ZoneId;
   }

   /// <summary>
   /// The resolved credentials for one domain: either an API token, or a Global API Key and email.
   /// </summary>
   private sealed record Credentials(string ApiToken, string AuthorizationKey, string AuthorizationEmail);
}