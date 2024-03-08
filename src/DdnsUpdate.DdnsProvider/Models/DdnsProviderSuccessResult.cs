// -------------------------------------------------------------------------
// <copyright file="DdnsProviderSuccessResult.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
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