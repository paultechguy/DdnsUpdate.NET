// "// <copyright file=\"IDdnsUpdateProvider.cs\" company=\"PaulTechGuy\">
// // Copyright (c) Paul Carver. All rights reserved.
// // </copyright>"

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
   /// The short, unique, text name of the DDNS provider that provides IP address updates. Used
   /// for logging purposes.
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
   /// <returns><see cref="DdnsProviderStatusResult"/>.</returns>
   Task<DdnsProviderStatusResult> IsDomainValidAsync(string domainName);

   /// <summary>
   /// Using the client, update the DNS for domainName with the specified ipAddress.
   /// </summary>
   /// <param name="domainName">The domain name to have a DNS updated (e.g. mycompany.com).</param>
   /// <param name="ipAddress">The IP address to use for a DNS update.</param>
   /// <returns><see cref="DdnsProviderStatusResult"/>.</returns>
   Task<DdnsProviderStatusResult> TryUpdateIpAddressAsync(
      string domainName,
      string ipAddress);

   /// <summary>
   /// This method is called before updating a batch of IP addresses, after waking up.
   /// <see cref="TryUpdateIpAddressAsync"/>.
   /// </summary>
   /// <returns></returns>
   Task BeginBatchUpdateIpAddressAsync();

   /// <summary>
   /// This method is called after updating a batch of IP addresses, after waking up.
   /// <see cref="TryUpdateIpAddressAsync"/>.
   /// </summary>
   /// <returns></returns>
   Task EndBatchUpdateIpAddressAsync();
}