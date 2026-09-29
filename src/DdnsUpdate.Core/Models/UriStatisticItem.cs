// -------------------------------------------------------------------------
// <copyright file="UriStatisticItem.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Models;

using System;

/// <summary>
/// Usage statistics for a single external IP address provider URL.
/// </summary>
public class UriStatisticItem
{
   /// <summary>
   /// Gets or sets a short display name, derived from the first label of the host name.
   /// </summary>
   public string Name { get; set; } = string.Empty;

   /// <summary>
   /// Gets or sets the provider URL, which returns the caller's IP address as text.
   /// </summary>
   public Uri Uri { get; set; } = new Uri("about:blank");

   /// <summary>
   /// Gets or sets the number of requests that returned a valid IP address.
   /// </summary>
   public int SuccessCount { get; set; } = 0;

   /// <summary>
   /// Gets or sets the number of requests that failed or returned no valid IP address.
   /// </summary>
   public int FailCount { get; set; } = 0;

   /// <summary>
   /// Gets or sets the UTC time of the most recent request, or null if never used.
   /// </summary>
   public DateTime? LastAttemptUtc { get; set; }

   /// <summary>
   /// Records a successful request.
   /// </summary>
   /// <returns>The new success count.</returns>
   public int IncrementSuccessCount()
   {
      ++this.SuccessCount;
      this.LastAttemptUtc = DateTime.UtcNow;

      return this.SuccessCount;
   }

   /// <summary>
   /// Records a failed request.
   /// </summary>
   /// <returns>The new failure count.</returns>
   public int IncrementFailCount()
   {
      ++this.FailCount;
      this.LastAttemptUtc = DateTime.UtcNow;

      return this.FailCount;
   }

   /// <summary>
   /// Creates an item with zeroed statistics from a URL string.
   /// </summary>
   /// <param name="value">An absolute URL.</param>
   /// <returns>The new <see cref="UriStatisticItem"/>.</returns>
   /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid URI.</exception>
   public static UriStatisticItem FromString(string value)
   {
      if (!Uri.TryCreate(value, default(UriCreationOptions), out Uri? uri))
      {
         throw new ArgumentException("Invalid URI", nameof(value));
      }

      var statsItem = new UriStatisticItem
      {
         // e.g. "https://api.ipify.org" => "api"
         Name = uri.Host.Split('.')[0].ToLower(),
         Uri = uri,
         SuccessCount = 0,
         FailCount = 0,
         LastAttemptUtc = null,
      };

      return statsItem;
   }
}