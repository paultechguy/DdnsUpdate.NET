// "// <copyright file="CloudflareDomain.cs\" company="PaulTechGuy"
// // Copyright (c) Paul Carver. All rights reserved.
// // </copyright>"

namespace DdnsUpdate.DdnsPlugin.Cloudflare.Models;

/// <summary>
/// A class representing a domain configuration to be used with a DDNS
/// plugin.
/// </summary>
public class CloudflareDomain
{
   /// <summary>
   /// Gets or sets the value indicating whether the domain DDNS should be updated.
   /// </summary>
   public bool IsEnabled { get; set; }

   /// <summary>
   /// Gets or sets the domain name (e.g mycompany.com).
   /// </summary>
   public string Name { get; set; }

   /// <summary>
   /// Gets or sets the Cloudflare DNS zone id.
   /// </summary>
   public string ZoneId { get; set; }

   /// <summary>
   /// Gets or sets the Cloudflare DNS record type.
   /// </summary>
   public string RecordId { get; set; }

   /// <summary>
   /// Gets or sets the Cloudflare DNS record type.
   /// </summary>
   public string RecordType { get; set; }

   /// <summary>
   /// Gets or sets the Cloudflare account authorization key.
   /// </summary>
   public string AuthorizationKey { get; set; }

   /// <summary>
   /// Gets or sets the Cloudflare account authorization email.
   /// </summary>
   public string AuthorizationEmail { get; set; }

   /// <summary>
   /// Creates a new instance of the <see cref="CloudflareDomain"/> class.
   /// </summary>
   public CloudflareDomain()
   {
      this.Name = string.Empty;
      this.ZoneId = string.Empty;
      this.RecordId = string.Empty;
      this.RecordType = string.Empty;
      this.AuthorizationKey = string.Empty;
      this.AuthorizationEmail = string.Empty;
   }
}
