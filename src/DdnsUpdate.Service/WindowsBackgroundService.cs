// -------------------------------------------------------------------------
// <copyright file="WindowsBackgroundService.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Service;

using DdnsUpdate.Core.Interfaces;
using DdnsUpdate.DdnsProvider.Helpers;
using DdnsUpdate.DdnsProvider.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class WindowsBackgroundService(
   IServiceProvider serviceProvider,
   IHostApplicationLifetime applicationLifetime,
   IPluginManager pluginManager,
   IConfiguration configuration,
   ILogger<WindowsBackgroundService> logger)
   : BackgroundService
{
   private readonly IServiceProvider serviceProvider = serviceProvider;
   private readonly IConfiguration configuration = configuration;
   private readonly IPluginManager pluginManager = pluginManager;
   private readonly IHostApplicationLifetime appLifetime = applicationLifetime;
   private readonly ILogger<WindowsBackgroundService> logger = logger;

   protected override async Task ExecuteAsync(CancellationToken cancelToken)
   {
      this.logger.LogDebug($"Starting {nameof(WindowsBackgroundService)}.{nameof(this.ExecuteAsync)}");

      try
      {
         // build plugin collection
         var plugins = this.LoadPlugins();

         if (plugins.Count > 0)
         {
            // execute each plugin
            var tasks = new List<Task>();
            foreach (var plugin in plugins)
            {
               var worker = this.serviceProvider.GetService<IWorkerService>();
               if (worker is null)
               {
                  throw new InvalidOperationException($"Unable to create service of type {nameof(IWorkerService)}");
               }

               // special case: we need to be sure the plugin is given a configuration in case it needs one
               plugin.SetConfiguration(this.configuration);

               var task = Task.Run(() => worker.ExecuteAsync(plugin, cancelToken), cancelToken);
               tasks.Add(task);
            }

            // wait for all tasks to complete
            await Task.WhenAll(tasks);
         }
         else
         {
            this.logger.LogWarning($"No plugins found; exiting");
         }

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
         this.logger.LogError($"Exception in {nameof(WindowsBackgroundService)}: {ex}");
      }

      this.logger.LogInformation($"Ending {nameof(WindowsBackgroundService)}.{nameof(this.ExecuteAsync)}");

      return;
   }

   private IList<IDdnsUpdateProvider> LoadPlugins()
   {
      // load all plugins from main plugin directory
      string topLevelPluginDirectory = FilePathHelper.ApplicationPluginDirectory;
      this.pluginManager.AddProviders(topLevelPluginDirectory, recursive: true);

      return this.pluginManager.Providers;
   }
}