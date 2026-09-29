// -------------------------------------------------------------------------
// <copyright file="CloudflareServiceCollectionExtensions.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.DdnsProvider.Cloudflare;

using DdnsUpdate.DdnsProvider.Cloudflare.Models;
using DdnsUpdate.DdnsProvider.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the Cloudflare DDNS provider.
/// </summary>
public static class CloudflareServiceCollectionExtensions
{
   private const string ConfigurationSectionName = "cloudflareSettings";

   /// <summary>
   /// Registers <see cref="DdnsUpdateProvider"/> as the <see cref="IDdnsUpdateProvider"/>, binds
   /// <see cref="CloudflareSettings"/> to the <c>cloudflareSettings</c> section, and configures the
   /// Cloudflare API <see cref="HttpClient"/>.
   /// </summary>
   /// <param name="services">The service collection.</param>
   /// <param name="configuration">The application configuration.</param>
   /// <returns>The <paramref name="services"/> for chaining.</returns>
   public static IServiceCollection AddCloudflareDdnsProvider(this IServiceCollection services, IConfiguration configuration)
   {
      _ = services.Configure<CloudflareSettings>(configuration.GetSection(ConfigurationSectionName));

      // the standard resilience handler retries transient failures (5xx, 408, 429, timeouts)
      // with backoff; repeating the same PATCH is safe because it sets the same values
      _ = services.AddHttpClient(DdnsUpdateProvider.HttpClientName, client =>
         {
            client.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");
         })
         .AddStandardResilienceHandler();

      _ = services.AddTransient<IDdnsUpdateProvider, DdnsUpdateProvider>();

      return services;
   }
}