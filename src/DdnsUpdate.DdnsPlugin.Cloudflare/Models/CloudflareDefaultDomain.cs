// <copyright file="CloudflareDefaultDomain.cs" company="PaulTechGuy"
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>"

namespace DdnsUpdate.DdnsPlugin.Cloudflare.Models;

/// <summary>
/// A class representing a default domain configuration to be used with a DDNS
/// plugin.  Values from this class are used when a instance of a
/// <see cref="CloudflareDomain"/> object has missing properties.
/// </summary>
public class CloudflareDefaultDomain
{
   /// <summary>
   /// Gets or sets the default Cloudflare DNS zone id.
   /// </summary>
   public string ZoneId { get; set; }

   /// <summary>
   /// Gets or sets the default Cloudflare DNS record type.
   /// </summary>
   public string RecordType { get; set; }

   /// <summary>
   /// Gets or sets the default Cloudflare account authorization key.
   /// </summary>
   public string AuthorizationKey { get; set; }

   /// <summary>
   /// Gets or sets the default Cloudflare account authorization email.
   /// </summary>
   public string AuthorizationEmail { get; set; }

   /// <summary>
   /// Creates a new instance of the <see cref="CloudflareDefaultDomain"/> class.
   /// </summary>
   public CloudflareDefaultDomain()
   {
      this.ZoneId = string.Empty;
      this.RecordType = string.Empty;
      this.AuthorizationKey = string.Empty;
      this.AuthorizationEmail = string.Empty;
   }
}
