// -------------------------------------------------------------------------
// <copyright file="CloudflareDefaultDomain.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.DdnsProvider.Cloudflare.Models;

/// <summary>
/// Fallback values for any <see cref="CloudflareDomain"/> property left empty, so shared values
/// such as the API credentials only need to be entered once.
/// </summary>
public class CloudflareDefaultDomain
{
   /// <summary>
   /// Gets or sets the default Cloudflare zone id.
   /// </summary>
   public string ZoneId { get; set; }

   /// <summary>
   /// Gets or sets the default Cloudflare DNS record type.
   /// </summary>
   public string RecordType { get; set; }

   /// <summary>
   /// Gets or sets the default Cloudflare Global API Key.
   /// </summary>
   public string AuthorizationKey { get; set; }

   /// <summary>
   /// Gets or sets the default Cloudflare account email address.
   /// </summary>
   public string AuthorizationEmail { get; set; }

   /// <summary>
   /// Initializes a new instance of the <see cref="CloudflareDefaultDomain"/> class.
   /// </summary>
   public CloudflareDefaultDomain()
   {
      this.ZoneId = string.Empty;
      this.RecordType = string.Empty;
      this.AuthorizationKey = string.Empty;
      this.AuthorizationEmail = string.Empty;
   }
}