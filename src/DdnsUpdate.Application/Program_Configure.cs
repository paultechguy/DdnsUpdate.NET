// -------------------------------------------------------------------------
// <copyright file="Program_Configure.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Application;

using System;
using DdnsUpdate.Core;
using DdnsUpdate.Core.Helpers;
using DdnsUpdate.Core.Interfaces;
using DdnsUpdate.Core.Models;
using DdnsUpdate.DdnsProvider.Interfaces;
using DdnsUpdate.Email;
using DdnsUpdate.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

public partial class Program
{
   private CommandLineOptions commandLineOptions;

   private bool disposed = false;

   public Program()
   {
      // keep compiler happy, but we'll overwrite this in Run()
      this.commandLineOptions = new CommandLineOptions();
   }

   public void Dispose()
   {
      // Dispose of unmanaged resources.
      this.Dispose(true);

      // Suppress finalization.
      GC.SuppressFinalize(this);
   }

   protected virtual void Dispose(bool disposing)
   {
      if (this.disposed)
      {
         return;
      }

      if (disposing)
      {
         this.cancelTokenSource.Dispose();
      }

      // free unmanaged resources (unmanaged objects) and override a finalizer below;
      // set large fields to null.

      this.disposed = true;
   }

   private void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
   {
      // app settings DI using IOptions pattern
      _ = services.Configure<ApplicationSettings>(hostContext.Configuration.GetSection(ApplicationSettings.ConfigurationName));

      // we need the lil client factory; remove http client logger since they tend to flood the logs
      // and are hard to disable specific levels
      _ = services.AddHttpClient();
      _ = services.ConfigureHttpClientDefaults(defaults =>
         defaults.RemoveAllLoggers());

      // normal DI stuff
      _ = services.AddTransient<IWorkerService, WorkerService>();
      _ = services.AddTransient<IEmailSender, EmailSender>();
      _ = services.AddSingleton(this.commandLineOptions);
      _ = services.AddHostedService<WindowsBackgroundService>();

      // DDNS update provider...sort of a big thing
      _ = services.AddTransient<IDdnsUpdateProvider, DdnsUpdate.DdnsProvider.Cloudflare.DdnsUpdateProvider>();

      // disable the default aspnet status messages that appear in console/log about startup
      _ = services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);
   }

   private IHostBuilder CreateHostBuilder()
   {
      // build configuration first so we can use it (e.g. default service name)
      IConfigurationRoot configuration = new ConfigurationBuilder()
         .SetBasePath(Directory.GetCurrentDirectory())
         .AddJsonFile($"appsettings.json", optional: false, reloadOnChange: true)
         .AddJsonFile($"appsettings.{this.dotnetEnvironmentName}.json", optional: true, reloadOnChange: true)
         .AddJsonFile($"appsettings.{this.dotnetEnvironmentName}.user.json", optional: true, reloadOnChange: true)
         .AddEnvironmentVariables()
         .AddCommandLine(Environment.GetCommandLineArgs())
         .Build();

      // this value really doesn't do much since the service name is passed in
      // via the CLI, Windows sc.exe command, for the various commands like
      // create, start, stop, etc.
      string? defaultServiceName = $"{FilePathHelper.ApplicationName} Service";

      IHostBuilder builder = Host.CreateDefaultBuilder()
         .ConfigureAppConfiguration(builder =>
         {
            builder.Sources.Clear();
            builder.AddConfiguration(configuration);
         })
         .ConfigureServices(this.ConfigureServices)
         .UseSerilog((hostContext, services, configuration) =>
         {
            configuration.WriteTo.File(
               $"{FilePathHelper.ApplicationDataDirectory}\\logs\\log_.txt",
               outputTemplate: "{Timestamp:MM/dd/yy HH:mm:ss.fff}|{Level:u3}|{Message}{NewLine}",
               rollingInterval: RollingInterval.Day,
               retainedFileCountLimit: 31,
               shared: true,
               flushToDiskInterval: TimeSpan.FromSeconds(1));

            // if we're in an interactive environment, add console output
            if (Environment.UserInteractive)
            {
               configuration.WriteTo.Console();
            }
         })
         .UseWindowsService(options =>
         {
            options.ServiceName = defaultServiceName ?? $"{FilePathHelper.ApplicationName} Service";
         });

      return builder;
   }
}