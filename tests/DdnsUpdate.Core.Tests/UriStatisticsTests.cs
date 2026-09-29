// -------------------------------------------------------------------------
// <copyright file="UriStatisticsTests.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Tests;

using DdnsUpdate.Core.Models;

public sealed class UriStatisticsTests : IDisposable
{
   private readonly string tempDirectory = Directory.CreateTempSubdirectory("DdnsUpdateTests").FullName;

   public void Dispose()
   {
      Directory.Delete(this.tempDirectory, recursive: true);
   }

   [Fact]
   public void FromString_DerivesNameFromFirstHostLabel()
   {
      UriStatisticItem item = UriStatisticItem.FromString("https://API.ipify.org");

      Assert.Equal("api", item.Name);
      Assert.Equal(0, item.SuccessCount);
      Assert.Equal(0, item.FailCount);
      Assert.Null(item.LastAttemptUtc);
   }

   [Fact]
   public void FromString_InvalidUri_Throws()
   {
      _ = Assert.Throws<ArgumentException>(() => UriStatisticItem.FromString("not a uri"));
   }

   [Fact]
   public void Merge_AddsOnlyUrisNotAlreadyPresent_IgnoringCaseAndTrailingSlash()
   {
      var stats = new UriStatistics(["https://ident.me/"]);

      stats.Merge(["https://IDENT.me", "https://api.ipify.org"]);

      Assert.Equal(2, stats.Count);
      Assert.Contains(stats, s => s.Uri == new Uri("https://api.ipify.org/"));
   }

   [Fact]
   public void Merge_KeepsExistingStatistics()
   {
      var stats = new UriStatistics(["https://ident.me/"]);
      _ = stats[0].IncrementSuccessCount();

      stats.Merge(["https://ident.me/"]);

      UriStatisticItem item = Assert.Single(stats);
      Assert.Equal(1, item.SuccessCount);
   }

   [Fact]
   public async Task ReadFileAsync_MissingFile_ReturnsEmpty()
   {
      UriStatistics stats = await UriStatistics.ReadFileAsync(Path.Combine(this.tempDirectory, "missing.json"));

      Assert.Empty(stats);
   }

   [Fact]
   public async Task WriteThenRead_RoundTripsWithMostRecentlyUsedFirst()
   {
      string path = Path.Combine(this.tempDirectory, "UriStatistics.json");
      var stats = new UriStatistics(["https://unused.example/", "https://older.example/", "https://newer.example/"]);
      _ = stats[1].IncrementFailCount();
      stats[1].LastAttemptUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
      _ = stats[2].IncrementSuccessCount();

      await stats.WriteFileAsync(path);
      UriStatistics read = await UriStatistics.ReadFileAsync(path);

      Assert.Equal(["newer", "older", "unused"], read.Select(s => s.Name));
      Assert.Equal(1, read[0].SuccessCount);
      Assert.Equal(1, read[1].FailCount);
   }
}