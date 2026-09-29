// -------------------------------------------------------------------------
// <copyright file="IWorkerService.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Interfaces;

using System.Threading.Tasks;

/// <summary>
/// The application's work, run by <see cref="WindowsBackgroundService"/>.
/// </summary>
public interface IWorkerService
{
   /// <summary>
   /// Runs the work. The application stops when the returned task completes.
   /// </summary>
   /// <param name="cancelToken">Signaled on service stop or Ctrl-C.</param>
   /// <returns>True if the run succeeded; false if it found problems that should make the process
   /// exit with a failure code (e.g. a dry run that found a misconfigured domain).</returns>
   Task<bool> ExecuteAsync(CancellationToken cancelToken);
}