// -------------------------------------------------------------------------
// <copyright file="DdnsProviderSuccessResult.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.DdnsProvider.Models;

/// <summary>
/// A class representing a genereralized result object with a success indicator
/// and a message.
/// </summary>
public class DdnsProviderSuccessResult
{
   /// <summary>
   /// Gets or sets the a value indicating whether the result is successful.
   /// </summary>
   public bool IsSuccess { get; set; } = false;

   /// <summary>
   /// Gets or sets the message of the result.  Genrally this is empty if the
   /// status is successful.
   /// </summary>
   public string Message { get; set; } = string.Empty;
}