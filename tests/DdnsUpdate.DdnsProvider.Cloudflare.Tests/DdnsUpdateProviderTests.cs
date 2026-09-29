// -------------------------------------------------------------------------
// <copyright file="DdnsUpdateProviderTests.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.DdnsProvider.Cloudflare.Tests;

using System.Net;
using System.Text.Json;
using DdnsUpdate.DdnsProvider.Cloudflare.Models;
using DdnsUpdate.DdnsProvider.Models;
using DdnsUpdate.Tests.Shared;

public sealed class DdnsUpdateProviderTests
{
   private static readonly Uri CloudflareBaseAddress = new("https://api.cloudflare.com/client/v4/");

   private readonly FakeHttpMessageHandler handler = new(_ => FakeHttpMessageHandler.Text(HttpStatusCode.OK, """{"success":true}"""));

   [Fact]
   public async Task GetDomainNamesAsync_ReturnsOnlyEnabledDomains()
   {
      DdnsUpdateProvider provider = this.CreateProvider(
         new CloudflareSettings
         {
            Domains =
            [
               new CloudflareDomain { IsEnabled = true, Name = "a.example.com" },
               new CloudflareDomain { IsEnabled = false, Name = "b.example.com" },
               new CloudflareDomain { IsEnabled = true, Name = "c.example.com" },
            ],
         });

      List<string> names = await provider.GetDomainNamesAsync();

      Assert.Equal(["a.example.com", "c.example.com"], names);
   }

   [Fact]
   public async Task IsDomainValidAsync_FillsEmptyValuesFromDefaultDomain()
   {
      DdnsUpdateProvider provider = await this.CreateInitializedProviderAsync(
         new CloudflareSettings
         {
            DefaultDomain = new CloudflareDefaultDomain { ZoneId = "zone", RecordType = "A", AuthorizationKey = "key", AuthorizationEmail = "me@example.com" },
            Domains = [new CloudflareDomain { IsEnabled = true, Name = "example.com", RecordId = "record" }],
         });

      DdnsProviderSuccessResult result = await provider.IsDomainValidAsync("example.com");

      Assert.True(result.IsSuccess, result.Message);
   }

   [Fact]
   public async Task IsDomainValidAsync_ListsEveryMissingValue()
   {
      DdnsUpdateProvider provider = await this.CreateInitializedProviderAsync(
         new CloudflareSettings { Domains = [new CloudflareDomain { IsEnabled = true, Name = "example.com" }] });

      DdnsProviderSuccessResult result = await provider.IsDomainValidAsync("example.com");

      Assert.False(result.IsSuccess);
      Assert.Contains("recordId", result.Message);
      Assert.Contains("recordType", result.Message);
      Assert.Contains("apiToken and authorizationKey", result.Message);
      Assert.Contains("zoneId", result.Message);
   }

   [Fact]
   public async Task IsDomainValidAsync_KeyWithoutEmail_ReportsMissingEmail()
   {
      DdnsUpdateProvider provider = await this.CreateInitializedProviderAsync(
         Settings(new CloudflareDomain { IsEnabled = true, Name = "example.com", RecordId = "record", ZoneId = "zone", RecordType = "A", AuthorizationKey = "key" }));

      DdnsProviderSuccessResult result = await provider.IsDomainValidAsync("example.com");

      Assert.False(result.IsSuccess);
      Assert.Equal("authorizationEmail is empty in both the domain and defaultDomain", result.Message);
   }

   [Fact]
   public async Task IsDomainValidAsync_ApiTokenNeedsNoKeyOrEmail()
   {
      DdnsUpdateProvider provider = await this.CreateInitializedProviderAsync(
         Settings(new CloudflareDomain { IsEnabled = true, Name = "example.com", RecordId = "record", ZoneId = "zone", RecordType = "A", ApiToken = "token" }));

      DdnsProviderSuccessResult result = await provider.IsDomainValidAsync("example.com");

      Assert.True(result.IsSuccess, result.Message);
   }

   [Fact]
   public async Task TryUpdateIpAddressAsync_WithGlobalApiKey_SendsPatchWithKeyHeaders()
   {
      DdnsUpdateProvider provider = await this.CreateInitializedProviderAsync(
         Settings(new CloudflareDomain { IsEnabled = true, Name = "example.com", RecordId = "record1", ZoneId = "zone1", RecordType = "A", AuthorizationKey = "key", AuthorizationEmail = "me@example.com" }));

      DdnsProviderSuccessResult result = await provider.TryUpdateIpAddressAsync("example.com", "203.0.113.7", TestContext.Current.CancellationToken);

      Assert.True(result.IsSuccess, result.Message);
      FakeHttpMessageHandler.RecordedRequest request = Assert.Single(this.handler.Requests);
      Assert.Equal(HttpMethod.Patch, request.Method);
      Assert.Equal(new Uri("https://api.cloudflare.com/client/v4/zones/zone1/dns_records/record1"), request.Uri);
      Assert.Equal("me@example.com", request.Headers["X-Auth-Email"]);
      Assert.Equal("key", request.Headers["X-Auth-Key"]);
      Assert.False(request.Headers.ContainsKey("Authorization"));

      // only type, name and content are sent, so fields such as "proxied" are left alone
      using JsonDocument body = JsonDocument.Parse(request.Body!);
      Assert.Equal(["type", "name", "content"], body.RootElement.EnumerateObject().Select(p => p.Name));
      Assert.Equal("A", body.RootElement.GetProperty("type").GetString());
      Assert.Equal("example.com", body.RootElement.GetProperty("name").GetString());
      Assert.Equal("203.0.113.7", body.RootElement.GetProperty("content").GetString());
   }

   [Fact]
   public async Task TryUpdateIpAddressAsync_WithApiToken_SendsBearerOnly()
   {
      DdnsUpdateProvider provider = await this.CreateInitializedProviderAsync(
         Settings(new CloudflareDomain { IsEnabled = true, Name = "example.com", RecordId = "record", ZoneId = "zone", RecordType = "A", ApiToken = "token", AuthorizationKey = "key", AuthorizationEmail = "me@example.com" }));

      _ = await provider.TryUpdateIpAddressAsync("example.com", "203.0.113.7", TestContext.Current.CancellationToken);

      FakeHttpMessageHandler.RecordedRequest request = Assert.Single(this.handler.Requests);
      Assert.Equal("Bearer token", request.Headers["Authorization"]);
      Assert.False(request.Headers.ContainsKey("X-Auth-Key"));
      Assert.False(request.Headers.ContainsKey("X-Auth-Email"));
   }

   [Fact]
   public async Task TryUpdateIpAddressAsync_DomainKeyBeatsDefaultToken()
   {
      var settings = Settings(new CloudflareDomain { IsEnabled = true, Name = "example.com", RecordId = "record", AuthorizationKey = "domain-key" });
      settings.DefaultDomain = new CloudflareDefaultDomain { ZoneId = "zone", RecordType = "A", ApiToken = "default-token", AuthorizationEmail = "me@example.com" };
      DdnsUpdateProvider provider = await this.CreateInitializedProviderAsync(settings);

      _ = await provider.TryUpdateIpAddressAsync("example.com", "203.0.113.7", TestContext.Current.CancellationToken);

      FakeHttpMessageHandler.RecordedRequest request = Assert.Single(this.handler.Requests);
      Assert.Equal("domain-key", request.Headers["X-Auth-Key"]);
      Assert.Equal("me@example.com", request.Headers["X-Auth-Email"]);
      Assert.False(request.Headers.ContainsKey("Authorization"));
   }

   [Fact]
   public async Task TryUpdateIpAddressAsync_DefaultTokenBeatsDefaultKey()
   {
      var settings = Settings(new CloudflareDomain { IsEnabled = true, Name = "example.com", RecordId = "record" });
      settings.DefaultDomain = new CloudflareDefaultDomain { ZoneId = "zone", RecordType = "A", ApiToken = "default-token", AuthorizationKey = "default-key", AuthorizationEmail = "me@example.com" };
      DdnsUpdateProvider provider = await this.CreateInitializedProviderAsync(settings);

      _ = await provider.TryUpdateIpAddressAsync("example.com", "203.0.113.7", TestContext.Current.CancellationToken);

      FakeHttpMessageHandler.RecordedRequest request = Assert.Single(this.handler.Requests);
      Assert.Equal("Bearer default-token", request.Headers["Authorization"]);
   }

   [Fact]
   public async Task TryUpdateIpAddressAsync_ErrorResponse_ReturnsFailureWithTrimmedBody()
   {
      var failingHandler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Text(HttpStatusCode.BadRequest, "{\"success\":false}\n"));
      DdnsUpdateProvider provider = new(
         new TestOptionsMonitor<CloudflareSettings>(Settings(new CloudflareDomain { IsEnabled = true, Name = "example.com", RecordId = "record", ZoneId = "zone", RecordType = "A", ApiToken = "token" })),
         new StubHttpClientFactory(failingHandler, CloudflareBaseAddress));

      DdnsProviderSuccessResult result = await provider.TryUpdateIpAddressAsync("example.com", "203.0.113.7", TestContext.Current.CancellationToken);

      Assert.False(result.IsSuccess);
      Assert.Equal("BadRequest: Bad Request; {\"success\":false}", result.Message);
   }

   [Fact]
   public async Task TryUpdateIpAddressAsync_UnknownDomain_FailsWithoutRequest()
   {
      DdnsUpdateProvider provider = await this.CreateInitializedProviderAsync(Settings());

      DdnsProviderSuccessResult result = await provider.TryUpdateIpAddressAsync("missing.example.com", "203.0.113.7", TestContext.Current.CancellationToken);

      Assert.False(result.IsSuccess);
      Assert.Empty(this.handler.Requests);
   }

   private static CloudflareSettings Settings(params CloudflareDomain[] domains)
   {
      return new CloudflareSettings { Domains = domains };
   }

   private DdnsUpdateProvider CreateProvider(CloudflareSettings settings)
   {
      return new DdnsUpdateProvider(
         new TestOptionsMonitor<CloudflareSettings>(settings),
         new StubHttpClientFactory(this.handler, CloudflareBaseAddress));
   }

   // the worker always calls GetDomainNamesAsync first in a pass, which snapshots the settings
   private async Task<DdnsUpdateProvider> CreateInitializedProviderAsync(CloudflareSettings settings)
   {
      DdnsUpdateProvider provider = this.CreateProvider(settings);
      _ = await provider.GetDomainNamesAsync();

      return provider;
   }
}