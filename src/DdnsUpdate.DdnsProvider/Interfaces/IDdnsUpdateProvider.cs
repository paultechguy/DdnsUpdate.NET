// -------------------------------------------------------------------------
// <copyright file="IDdnsUpdateProvider.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
// -------------------------------------------------------------------------

namespace DdnsUpdate.DdnsProvider.Interfaces;

using System.Threading.Tasks;
using DdnsUpdate.DdnsProvider.Models;

/// <summary>
/// An interface representing the contract for a DDNS provided that updates domain
/// DNS records.
/// </summary>
public interface IDdnsUpdateProvider : IDisposable
{
   /// <summary>
   /// The text name of the DDNS provider that provides IP address updates.
   /// </summary>
   string ProviderName { get; set; }

   /// <summary>
   /// Get all the domain names that need a DNS update using the latest IP address.
   /// </summary>
   /// <returns>A <see cref="List"/> of string domain names.</returns>
   Task<List<string>> GetDomainNamesAsync();

   /// <summary>
   /// Determines if the domain referred to by domainName, is valid for just prior
   /// IP address to be updated.  This method can perform any internal configuration for
   /// the domain.  This method is called prior to
   /// <see cref="TryUpdateIpAddressAsync(HttpClient, string, string)"/> for each domain
   /// to be updated.
   /// </summary>
   /// <param name="domainName">The domain name to validate.</param>
   /// <returns><see cref="DdnsProviderSuccessResult"/>.</returns>
   Task<DdnsProviderSuccessResult> IsDomainValidAsync(string domainName);

   /// <summary>
   /// Using the client, update the DNS for domainName with the specified ipAddress.
   /// </summary>
   /// <param name="domainName">The domain name to have a DNS updated (e.g. mycompany.com).</param>
   /// <param name="ipAddress">The IP address to use for a DNS update.</param>
   /// <returns><see cref="DdnsProviderSuccessResult"/>.</returns>
   Task<DdnsProviderSuccessResult> TryUpdateIpAddressAsync(
      string domainName,
      string ipAddress);
}