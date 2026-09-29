// -------------------------------------------------------------------------
// <copyright file="UriStatistics.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Models;

using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// The set of external IP address provider URLs together with their success and failure
/// counts, persisted between runs as <c>UriStatistics.json</c>.
/// </summary>
public class UriStatistics : List<UriStatisticItem>
{
   /// <summary>
   /// Initializes a new, empty instance of the <see cref="UriStatistics"/> class.
   /// </summary>
   public UriStatistics()
   {
   }

   /// <summary>
   /// Initializes a new instance of the <see cref="UriStatistics"/> class with zeroed
   /// statistics for each URI.
   /// </summary>
   /// <param name="uris">The provider URIs.</param>
   public UriStatistics(ICollection<string> uris)
   {
      this.AddRange(uris
         .Select(x => UriStatisticItem.FromString(x)));
   }

   /// <summary>
   /// Adds any URIs not already present (compared case-insensitively after normalization).
   /// Existing items and their statistics are kept, even if absent from <paramref name="uris"/>.
   /// </summary>
   /// <param name="uris">The provider URIs, typically from configuration.</param>
   public void Merge(ICollection<string> uris)
   {
      // normalize through Uri so "https://ident.me" and "https://ident.me/" match
      foreach (string uri in uris.Select(u => new Uri(u).ToString()))
      {
         if (!this.Any(x => x.Uri.ToString().Equals(uri, StringComparison.OrdinalIgnoreCase)))
         {
            this.Add(UriStatisticItem.FromString(uri));
         }
      }
   }

   /// <summary>
   /// Reads statistics from a JSON file.
   /// </summary>
   /// <param name="path">The file path.</param>
   /// <returns>The statistics, or an empty collection if the file does not exist.</returns>
   /// <exception cref="InvalidOperationException">The file contains JSON <c>null</c>.</exception>
   public static async Task<UriStatistics> ReadFileAsync(string path)
   {
      if (!Path.Exists(path))
      {
         return [];
      }

      path = Path.GetFullPath(path);
      string json = await File.ReadAllTextAsync(path);
      UriStatistics stats = JsonSerializer.Deserialize<UriStatistics>(json)
         ?? throw new InvalidOperationException($"Unable to read {nameof(UriStatistics)} from file: {path}");

      return stats;
   }

   /// <summary>
   /// Writes the statistics to a JSON file, most recently attempted provider first.
   /// </summary>
   /// <param name="path">The file path.</param>
   /// <param name="pretty">Whether to indent the JSON for readability.</param>
   /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
   public async Task WriteFileAsync(string path, bool pretty = true)
   {
      path = Path.GetFullPath(path);

      // sort in json file by most recently used; if unused...put last in file
      IOrderedEnumerable<UriStatisticItem> sortedStats = this
         .OrderByDescending(x => x.LastAttemptUtc is null ? DateTime.MinValue : x.LastAttemptUtc);

      var options = new JsonSerializerOptions { WriteIndented = pretty };
      await File.WriteAllTextAsync(path, JsonSerializer.Serialize(sortedStats, options));
   }
}