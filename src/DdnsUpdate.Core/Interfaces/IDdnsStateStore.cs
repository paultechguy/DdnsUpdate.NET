// -------------------------------------------------------------------------
// <copyright file="IDdnsStateStore.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Interfaces;

using DdnsUpdate.Core.Models;

/// <summary>
/// Persists the worker's state between runs: the last IP address pushed to DNS and the
/// IP address provider statistics.
/// </summary>
/// <remarks>
/// The production implementation writes files under
/// <see cref="Helpers.FilePathHelper.ApplicationDataDirectory"/>; tests use an in-memory store.
/// </remarks>
public interface IDdnsStateStore
{
   /// <summary>
   /// Gets the last IP address that was detected and saved.
   /// </summary>
   /// <returns>The IP address, or an empty string if none has been saved.</returns>
   string LoadLastIpAddress();

   /// <summary>
   /// Saves the IP address as the last one detected.
   /// </summary>
   /// <param name="ipAddress">The IP address.</param>
   void SaveLastIpAddress(string ipAddress);

   /// <summary>
   /// Loads the saved IP address provider statistics.
   /// </summary>
   /// <returns>The statistics, or an empty collection if none have been saved.</returns>
   Task<UriStatistics> LoadUriStatisticsAsync();

   /// <summary>
   /// Saves the IP address provider statistics.
   /// </summary>
   /// <param name="statistics">The statistics to save.</param>
   /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
   Task SaveUriStatisticsAsync(UriStatistics statistics);
}