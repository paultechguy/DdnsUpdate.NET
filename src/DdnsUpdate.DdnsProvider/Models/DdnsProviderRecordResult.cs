// -------------------------------------------------------------------------
// <copyright file="DdnsProviderRecordResult.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.DdnsProvider.Models;

/// <summary>
/// The outcome of reading a domain's DNS record from a DDNS provider.
/// </summary>
public class DdnsProviderRecordResult : DdnsProviderSuccessResult
{
   /// <summary>
   /// Gets or sets the record's current value (the IP address it points to), when the read
   /// succeeded.
   /// </summary>
   public string? CurrentIpAddress { get; set; }
}