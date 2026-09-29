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
   /// <returns>A <see cref="Task"/> that completes when the work is finished or canceled.</returns>
   Task ExecuteAsync(CancellationToken cancelToken);
}