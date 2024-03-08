// -------------------------------------------------------------------------
// <copyright file="UriStatistics.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Models;

using System.Collections.Generic;
using System.Text.Json;

public class UriStatistics : List<UriStatisticItem>
{
   public UriStatistics()
   {
   }

   public UriStatistics(ICollection<string> uris)
   {
      this.AddRange(uris
         .Select(x => UriStatisticItem.FromString(x)));
   }

   public void Merge(ICollection<string> uris)
   {
      foreach (string uri in uris.Select(u => new Uri(u).ToString()))
      {
         if (!this.Any(x => x.Uri.ToString().Equals(uri, StringComparison.OrdinalIgnoreCase)))
         {
            this.Add(UriStatisticItem.FromString(uri));
         }
      }
   }

   public static async Task<UriStatistics> ReadFileAsync(string path)
   {
      if (!Path.Exists(path))
      {
         return [];
      }

      try
      {
         path = Path.GetFullPath(path);
         string json = await File.ReadAllTextAsync(path);
         UriStatistics stats = JsonSerializer.Deserialize<UriStatistics>(json)
            ?? throw new InvalidOperationException($"Unable to read {nameof(UriStatistics)} from file: {path}");

         return stats;
      }
      catch (Exception)
      {
         throw;
      }
   }

   public async Task WriteFileAsync(string path, bool pretty = true)
   {
      try
      {
         path = Path.GetFullPath(path);

         // sort in json file by most recently used; if unused...put last in file
         IOrderedEnumerable<UriStatisticItem> sortedStats = this
            .OrderByDescending(x => x.LastAttemptUtc is null ? DateTime.MinValue : x.LastAttemptUtc);

         var options = new JsonSerializerOptions { WriteIndented = pretty };
         await File.WriteAllTextAsync(path, JsonSerializer.Serialize(sortedStats, options));
      }
      catch (Exception)
      {
         throw;
      }
   }
}