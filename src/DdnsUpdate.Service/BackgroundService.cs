// <copyright file="BackgroundService.cs" company="PaulTechGuy"
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>"

namespace DdnsUpdate.Service;

using DdnsUpdate.Core.Extensions;
using DdnsUpdate.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class BackgroundService(
   IIpAddressMonitorService appService,
   IHostApplicationLifetime applicationLifetime,
   IConfiguration configuration,
   ILogger<BackgroundService> logger)
   : Microsoft.Extensions.Hosting.BackgroundService
{
   private readonly IIpAddressMonitorService appService = appService;
   private readonly IConfiguration configuration = configuration;
   private readonly IHostApplicationLifetime appLifetime = applicationLifetime;
   private readonly ILogger<BackgroundService> logger = logger;

   protected override async Task ExecuteAsync(CancellationToken cancelToken)
   {
      this.logger.LogAny(LogLevel.Debug, $"Starting {nameof(BackgroundService)}.{nameof(this.ExecuteAsync)}");

      try
      {
         await this.appService.ExecuteAsync(cancelToken);

         // we need to stop the hosted service, which will ensure application exit
         this.appLifetime.StopApplication();
      }
      catch (OperationCanceledException)
      {
         // When the stopping token is canceled, for example, a call made from services.msc,
         // we shouldn't exit with a non-zero exit code. In other words, this is expected...
      }
      catch (Exception ex)
      {
         this.logger.LogAny(LogLevel.Error, $"Exception in {nameof(BackgroundService)}: {ex}");
      }

      this.logger.LogAny(LogLevel.Information, $"Ending {nameof(BackgroundService)}.{nameof(this.ExecuteAsync)}");

      return;
   }
}
