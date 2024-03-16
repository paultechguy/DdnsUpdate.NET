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
using Microsoft.Extensions.Configuration;

/// <summary>
/// An interface representing the contract for a DDNS provided that updates domain
/// DNS records.
/// </summary>
public interface IDdnsUpdateProvider
{
   /// <summary>
   /// The text name of the DDNS provider that provides IP address updates.
   /// </summary>
   string ProviderName { get; set; }

   /// <summary>
   /// Provides an <see cref="IConfiguration"/> interface to the provider.  This will
   /// occur before any other methods are called.
   /// </summary>
   /// <param name="configuration"></param>
   void SetConfiguration(IConfiguration configuration);

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
   /// <param name="client"><see cref="HttpClient"/>. Do not Dispose of the client; it will be
   /// done for you.</param>
   /// <param name="domainName">The domain name to have a DNS updated (e.g. mycompany.com).</param>
   /// <param name="ipAddress">The IP address to use for a DNS update.</param>
   /// <returns><see cref="DdnsProviderSuccessResult"/>.</returns>
   Task<DdnsProviderSuccessResult> TryUpdateIpAddressAsync(
      HttpClient client,
      string domainName,
      string ipAddress);
}