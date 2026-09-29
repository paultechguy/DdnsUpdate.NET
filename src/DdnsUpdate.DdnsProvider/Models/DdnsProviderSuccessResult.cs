// -------------------------------------------------------------------------
// <copyright file="DdnsProviderSuccessResult.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.DdnsProvider.Models;

/// <summary>
/// The outcome of a DDNS provider operation. Providers report failures through this result
/// rather than by throwing.
/// </summary>
public class DdnsProviderSuccessResult
{
   /// <summary>
   /// Gets or sets a value indicating whether the operation succeeded.
   /// </summary>
   public bool IsSuccess { get; set; } = false;

   /// <summary>
   /// Gets or sets the failure description. Generally this is empty if the operation succeeded.
   /// </summary>
   public string Message { get; set; } = string.Empty;
}