// -------------------------------------------------------------------------
// <copyright file="WindowsBackgroundService.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core;

using DdnsUpdate.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// The hosted service that runs <see cref="IWorkerService"/> and stops the host when it finishes,
/// so the same code works as a Windows Service, a scheduled task, or a console app.
/// </summary>
public sealed class WindowsBackgroundService(
   IWorkerService appService,
   IHostApplicationLifetime applicationLifetime,
   ILogger<WindowsBackgroundService> logger)
   : BackgroundService
{
   private readonly IWorkerService appService = appService;
   private readonly IHostApplicationLifetime appLifetime = applicationLifetime;
   private readonly ILogger<WindowsBackgroundService> logger = logger;

   /// <inheritdoc/>
   protected override async Task ExecuteAsync(CancellationToken cancelToken)
   {
      this.logger.LogDebug($"Starting {nameof(WindowsBackgroundService)}.{nameof(this.ExecuteAsync)}");

      try
      {
         await this.appService.ExecuteAsync(cancelToken);

         // the worker finished on its own (e.g. maximum iterations reached); without this the
         // host would keep running with nothing to do
         this.appLifetime.StopApplication();
      }
      catch (OperationCanceledException)
      {
         // When the stopping token is canceled, for example, a call made from services.msc,
         // we shouldn't exit with a non-zero exit code. In other words, this is expected...
      }
      catch (Exception ex)
      {
         this.logger.LogError($"Exception in {nameof(WindowsBackgroundService)}: {ex}");
      }

      this.logger.LogInformation($"Ending {nameof(WindowsBackgroundService)}.{nameof(this.ExecuteAsync)}");

      return;
   }
}