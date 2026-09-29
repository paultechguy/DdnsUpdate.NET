// -------------------------------------------------------------------------
// <copyright file="IDdnsUpdateProvider.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.DdnsProvider.Interfaces;

using System.Threading.Tasks;
using DdnsUpdate.DdnsProvider.Models;

/// <summary>
/// The contract for a DDNS provider that updates domain DNS records with an IP address.
/// </summary>
/// <remarks>
/// Each update pass calls <see cref="GetDomainNamesAsync"/> first, then, for each domain,
/// <see cref="IsDomainValidAsync(string)"/> followed by
/// <see cref="TryUpdateIpAddressAsync(HttpClient, string, string)"/>. Domain updates may run
/// in parallel. Providers read their own configuration section.
/// </remarks>
public interface IDdnsUpdateProvider
{
   /// <summary>
   /// Gets the display name of the DDNS provider.
   /// </summary>
   public string ProviderName { get; }

   /// <summary>
   /// Gets the names of all enabled domains to update with the latest IP address.
   /// </summary>
   /// <returns>A <see cref="List{T}"/> of domain names.</returns>
   Task<List<string>> GetDomainNamesAsync();

   /// <summary>
   /// Determines whether the domain is configured well enough to be updated. This method can
   /// also perform any internal configuration for the domain.
   /// </summary>
   /// <param name="domainName">The domain name to validate.</param>
   /// <returns>A <see cref="DdnsProviderSuccessResult"/> whose message lists any problems.</returns>
   Task<DdnsProviderSuccessResult> IsDomainValidAsync(string domainName);

   /// <summary>
   /// Using the client, updates the DNS record for the domain with the specified IP address.
   /// </summary>
   /// <param name="client">A <see cref="HttpClient"/> for this domain only; the provider may set
   /// its default headers. Do not dispose of the client.</param>
   /// <param name="domainName">The domain name whose DNS record is updated (e.g. mycompany.com).</param>
   /// <param name="ipAddress">The IP address to use for the DNS update.</param>
   /// <returns>A <see cref="DdnsProviderSuccessResult"/>; failures are reported here rather
   /// than thrown.</returns>
   Task<DdnsProviderSuccessResult> TryUpdateIpAddressAsync(
      HttpClient client,
      string domainName,
      string ipAddress);
}