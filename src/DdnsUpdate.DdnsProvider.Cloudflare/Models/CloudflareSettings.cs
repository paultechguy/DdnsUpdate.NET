// -------------------------------------------------------------------------
// <copyright file="CloudflareSettings.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.DdnsProvider.Cloudflare.Models;

/// <summary>
/// The Cloudflare provider settings, bound from the top-level <c>cloudflareSettings</c> section.
/// </summary>
public class CloudflareSettings
{
   /// <summary>
   /// The name of the <see cref="CloudflareSettings"/> configuration section in
   /// the appsettings file(s).
   /// </summary>
   public const string ConfigurationName = "CloudflareSettings";

   /// <summary>
   /// Gets or sets the <see cref="CloudflareDefaultDomain"/> values to use when
   /// the corresponding <see cref="Domains"/> values are empty.
   /// </summary>
   public CloudflareDefaultDomain DefaultDomain { get; set; }

   /// <summary>
   /// Gets or sets the DNS records to update; only those with
   /// <see cref="CloudflareDomain.IsEnabled"/> set are used.
   /// </summary>
   public CloudflareDomain[] Domains { get; set; }

   /// <summary>
   /// Initializes a new instance of the <see cref="CloudflareSettings"/> class.
   /// </summary>
   public CloudflareSettings()
   {
      this.DefaultDomain = new CloudflareDefaultDomain();
      this.Domains = [];
   }
}