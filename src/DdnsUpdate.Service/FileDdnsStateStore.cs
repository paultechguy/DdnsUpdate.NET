// -------------------------------------------------------------------------
// <copyright file="FileDdnsStateStore.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Service;

using DdnsUpdate.Core.Helpers;
using DdnsUpdate.Core.Interfaces;
using DdnsUpdate.Core.Models;

/// <summary>
/// An <see cref="IDdnsStateStore"/> that keeps <c>LastIpAddress.txt</c> and
/// <c>UriStatistics.json</c> in <see cref="FilePathHelper.ApplicationDataDirectory"/>.
/// </summary>
public sealed class FileDdnsStateStore : IDdnsStateStore
{
   private const string LastIpAddressFileName = "LastIpAddress.txt";
   private const string UriStatisticsFileName = "UriStatistics.json";

   private readonly string lastIpAddressFilePath = Path.Combine(FilePathHelper.ApplicationDataDirectory, LastIpAddressFileName);
   private readonly string uriStatisticsFilePath = Path.Combine(FilePathHelper.ApplicationDataDirectory, UriStatisticsFileName);

   /// <inheritdoc/>
   public string LoadLastIpAddress()
   {
      return File.Exists(this.lastIpAddressFilePath) ? File.ReadAllText(this.lastIpAddressFilePath) : string.Empty;
   }

   /// <inheritdoc/>
   public void SaveLastIpAddress(string ipAddress)
   {
      _ = Directory.CreateDirectory(FilePathHelper.ApplicationDataDirectory);
      File.WriteAllText(this.lastIpAddressFilePath, ipAddress);
   }

   /// <inheritdoc/>
   public Task<UriStatistics> LoadUriStatisticsAsync()
   {
      return UriStatistics.ReadFileAsync(this.uriStatisticsFilePath);
   }

   /// <inheritdoc/>
   public Task SaveUriStatisticsAsync(UriStatistics statistics)
   {
      _ = Directory.CreateDirectory(FilePathHelper.ApplicationDataDirectory);
      return statistics.WriteFileAsync(this.uriStatisticsFilePath);
   }
}