// -------------------------------------------------------------------------
// <copyright file="CloudflareDomain.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.DdnsProvider.Cloudflare.Models;

/// <summary>
/// One Cloudflare DNS record to keep updated. Empty values (other than the name and record id)
/// fall back to <see cref="CloudflareSettings.DefaultDomain"/>.
/// </summary>
public class CloudflareDomain
{
   /// <summary>
   /// Gets or sets a value indicating whether the domain DDNS should be updated.
   /// </summary>
   public bool IsEnabled { get; set; }

   /// <summary>
   /// Gets or sets the DNS record name (e.g. mycompany.com or home.mycompany.com).
   /// </summary>
   public string Name { get; set; }

   /// <summary>
   /// Gets or sets the Cloudflare zone id, shown on the domain's Overview page.
   /// </summary>
   public string ZoneId { get; set; }

   /// <summary>
   /// Gets or sets the Cloudflare DNS record id. It is not shown in the dashboard; see the
   /// README for how to look it up through the API.
   /// </summary>
   public string RecordId { get; set; }

   /// <summary>
   /// Gets or sets the Cloudflare DNS record type (normally "A").
   /// </summary>
   public string RecordType { get; set; }

   /// <summary>
   /// Gets or sets the Cloudflare Global API Key.
   /// </summary>
   public string AuthorizationKey { get; set; }

   /// <summary>
   /// Gets or sets the email address of the Cloudflare account that owns the API key.
   /// </summary>
   public string AuthorizationEmail { get; set; }

   /// <summary>
   /// Initializes a new instance of the <see cref="CloudflareDomain"/> class.
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