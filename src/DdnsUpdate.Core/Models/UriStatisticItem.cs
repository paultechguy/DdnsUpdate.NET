// "// <copyright file=\"UriStatisticItem.cs\" company=\"PaulTechGuy\">
// // Copyright (c) Paul Carver. All rights reserved.
// // </copyright>"

namespace DdnsUpdate.Core.Models;

using System;

public class UriStatisticItem
{
   public string Name { get; set; } = string.Empty;

   public Uri Uri { get; set; } = new Uri("about:blank");

   public int SuccessCount { get; set; } = 0;

   public int FailCount { get; set; } = 0;

   public DateTime? LastAttemptUtc { get; set; }

   public int IncrementSuccessCount()
   {
      ++this.SuccessCount;
      this.LastAttemptUtc = DateTime.UtcNow;

      return this.SuccessCount;
   }

   public int IncrementFailCount()
   {
      ++this.FailCount;
      this.LastAttemptUtc = DateTime.UtcNow;

      return this.FailCount;
   }

   public static UriStatisticItem FromString(string value)
   {
      if (!Uri.TryCreate(value, default(UriCreationOptions), out Uri? uri))
      {
         throw new ArgumentException("Invalid URI", nameof(value));
      }

      var statsItem = new UriStatisticItem
      {
         Name = uri.Host.Split('.')[0].ToLower(),
         Uri = uri,
         SuccessCount = 0,
         FailCount = 0,
         LastAttemptUtc = null,
      };

      return statsItem;
   }
}
