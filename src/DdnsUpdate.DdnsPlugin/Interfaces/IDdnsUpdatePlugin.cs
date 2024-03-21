// <copyright file="IDdnsUpdatePlugin.cs" company="PaulTechGuy"
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>"

namespace DdnsUpdate.DdnsPlugin.Interfaces;

using System.Threading.Tasks;
using DdnsUpdate.DdnsPlugin.Models;

/// <summary>
/// An interface representing the contract for a DDNS provided that updates domain
/// DNS records.
/// </summary>
public interface IDdnsUpdatePlugin : IDisposable
{
   /// <summary>
   /// The short, unique, text name of the DDNS plugin that provides IP address updates.  This is used
   /// in log output.
   /// </summary>
   string PluginName { get; set; }

   /// <summary>
   /// Get all the domain names that need a DNS update using the latest IP address.
   /// </summary>
   /// <returns>A <see cref="List"/> of string domain names.</returns>
   Task<List<string>> GetDomainNamesAsync();

   /// <summary>
   /// This method is called before updating a batch of IP addresses, after waking up.
   /// </summary>
   /// <returns><see cref="Task"/>.</returns>
   Task BeginBatchUpdateIpAddressAsync();

   /// <summary>
   /// This method is called after updating a batch of IP addresses, after waking up.
   /// <see cref="TryUpdateIpAddressAsync"/>.
   /// </summary>
   /// <returns></returns>
   Task EndBatchUpdateIpAddressAsync();
   /// <summary>
   /// Using the client, update the DNS for domainName with the specified ipAddress.
   /// </summary>
   /// <param name="domainName">The domain name to have a DNS updated (e.g. mycompany.com).</param>
   /// <param name="ipAddress">The IP address to use for a DNS update.</param>
   /// <returns><see cref="DdnsPluginStatusResult"/>.</returns>
   Task<DdnsPluginStatusResult> TryUpdateIpAddressAsync(
      string domainName,
      string ipAddress);
}
