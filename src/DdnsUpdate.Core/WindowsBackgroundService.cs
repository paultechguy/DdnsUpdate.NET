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
      this.logger.LogDebug("Starting {Class}.{Method}", nameof(WindowsBackgroundService), nameof(this.ExecuteAsync));

      try
      {
         if (!await this.appService.ExecuteAsync(cancelToken))
         {
            // e.g. a dry run that found a problem; report it to scripts through the exit code
            Environment.ExitCode = 1;
         }
      }
      catch (OperationCanceledException)
      {
         // When the stopping token is canceled, for example, a call made from services.msc,
         // we shouldn't exit with a non-zero exit code. In other words, this is expected...
      }
      catch (Exception ex)
      {
         this.logger.LogError(ex, "Exception in {Class}", nameof(WindowsBackgroundService));

         // a non-zero exit code lets Windows Service recovery options restart the service
         Environment.ExitCode = 1;
      }
      finally
      {
         // whether the worker finished on its own (e.g. maximum iterations reached) or failed,
         // stop the host; otherwise it would keep running as a zombie with nothing to do
         this.appLifetime.StopApplication();
      }

      this.logger.LogInformation("Ending {Class}.{Method}", nameof(WindowsBackgroundService), nameof(this.ExecuteAsync));
   }
}