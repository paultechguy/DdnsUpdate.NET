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
using DdnsUpdate.Core.Models;
using DdnsUpdate.DdnsProvider.Cloudflare;
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

   /// <summary>
   /// Initializes a new instance of the <see cref="Program"/> class.
   /// </summary>
   public Program()
   {
      // keep compiler happy, but we'll overwrite this in Run()
      this.commandLineOptions = new CommandLineOptions();
   }

   /// <inheritdoc/>
   public void Dispose()
   {
      // Dispose of unmanaged resources.
      this.Dispose(true);

      // Suppress finalization.
      GC.SuppressFinalize(this);
   }

   /// <summary>
   /// Releases the cancellation token source.
   /// </summary>
   /// <param name="disposing">True when called from <see cref="Dispose()"/>.</param>
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
   /// <summary>
   /// Builds the generic host: configuration, services, logging and Windows Service support.
   /// </summary>
   /// <param name="args">The command-line arguments.</param>
   /// <returns>The built <see cref="IHost"/>.</returns>
   private IHost CreateHost(string[] args)
   {
      // defaults are disabled so the configuration sources and logging are exactly as below
      HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
      {
         Args = args,
         DisableDefaults = true,
         EnvironmentName = this.dotnetEnvironmentName,
         ContentRootPath = Directory.GetCurrentDirectory(),
      });

      // later sources override earlier ones, so the git-ignored *.user.json file holds secrets
      // and per-server values on top of the committed appsettings files
      _ = builder.Configuration
         .SetBasePath(builder.Environment.ContentRootPath)
         .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
         .AddJsonFile($"appsettings.{this.dotnetEnvironmentName}.json", optional: true, reloadOnChange: true)
         .AddJsonFile($"appsettings.{this.dotnetEnvironmentName}.user.json", optional: true, reloadOnChange: true)
         .AddEnvironmentVariables()
         .AddCommandLine(args);

      this.ConfigureServices(builder.Services, builder.Configuration);

      return builder.Build();
   }

   private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
   {
      // app settings DI using the options pattern; consumers use IOptionsMonitor so edits to the
      // settings files apply without a restart
      _ = services.AddOptions<ApplicationSettings>()
         .Bind(configuration.GetSection(ApplicationSettings.ConfigurationName))
         .Validate(
            settings => AreIpAddressProvidersValid(settings.DdnsSettings.IpAddressProviders),
            "applicationSettings.ddnsSettings.ipAddressProviders must contain at least one absolute http(s) URL")
         .ValidateOnStart();

      // remove http client loggers since they tend to flood the logs and are hard to disable
      // at specific levels
      _ = services.ConfigureHttpClientDefaults(defaults =>
         defaults.RemoveAllLoggers());

      // the application's parts; each project registers its own services
      _ = services
         .AddDdnsUpdateWorker()
         .AddSmtpEmailSender()
         .AddSingleton(this.commandLineOptions)
         .AddHostedService<WindowsBackgroundService>();

      // DDNS update provider...sort of a big thing; register a different IDdnsUpdateProvider
      // here to support another DNS service
      _ = services.AddCloudflareDdnsProvider(configuration);

      // disable the default status messages that appear in console/log about startup
      _ = services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);

      // when running as a Windows Service, integrate with the Service Control Manager;
      // otherwise this does nothing. The name only matters for event log entries, since
      // sc.exe sets the actual service name.
      _ = services.AddWindowsService(options =>
      {
         options.ServiceName = $"{FilePathHelper.ApplicationName} Service";
      });

      _ = services.AddSerilog((services, loggerConfiguration) =>
      {
         // the resilience handler logs every HTTP attempt at Information; keep only its problems
         _ = loggerConfiguration.MinimumLevel.Override("Polly", Serilog.Events.LogEventLevel.Warning);

         // {Message:lj} renders string values without quotes, matching the console output
         _ = loggerConfiguration.WriteTo.File(
            Path.Combine(FilePathHelper.ApplicationDataDirectory, "logs", "log_.txt"),
            outputTemplate: "{Timestamp:MM/dd/yy HH:mm:ss.fff}|{Level:u3}|{Message:lj}{NewLine}{Exception}",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 31,
            shared: true,
            flushToDiskInterval: TimeSpan.FromSeconds(1));

         // if we're in an interactive environment, add console output
         if (Environment.UserInteractive)
         {
            _ = loggerConfiguration.WriteTo.Console();
         }
      });
   }

   private static bool AreIpAddressProvidersValid(string[] providers)
   {
      return providers.Length > 0
         && providers.All(p => Uri.TryCreate(p, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
   }
}