// -------------------------------------------------------------------------
// <copyright file="CloudflareSettings.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.DdnsProvider.Cloudflare.Models;

/// <summary>
/// A class representing the appSettings configuration section for the Cloudflare
/// <see cref="DdnsUpdateProvider"/>.
/// </summary>
public class CloudflareSettings
{
   /// <summary>
   /// The name of the <see cref="CloudflareSettings"/> configuration section in
   /// the appSettings file(s).
   /// </summary>
   public const string ConfigurationName = "CloudflareSettings";

   /// <summary>
   /// The <see cref="CloudflareDefaultDomain"/> object properties to use when
   /// the Domains object properties do not contain a value.
   /// </summary>
   public CloudflareDefaultDomain DefaultDomain { get; set; }

   /// <summary>
   /// A collection of <see cref="CloudflareDomain"/> object configurations.
   /// </summary>
   public CloudflareDomain[] Domains { get; set; }

   /// <summary>
   /// Creates a new instance of the <see cref="CloudflareSettings"/> class.
   /// </summary>
   public CloudflareSettings()
   {
      this.DefaultDomain = new CloudflareDefaultDomain();
      this.Domains = [];
   }
}