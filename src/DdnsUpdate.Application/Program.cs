// -------------------------------------------------------------------------
// <copyright file="Program.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Application;

using System;
using System.Reflection;
using System.Threading;
using CommandLine;
using DdnsUpdate.Application.Helpers;
using DdnsUpdate.Core.Helpers;
using DdnsUpdate.Core.Models;
using Microsoft.Extensions.Hosting;
using Serilog;

/// <summary>
/// The application entry point: parses the command line, determines the .NET environment,
/// configures logging, and runs the generic host (see Program_Configure.cs).
/// </summary>
public partial class Program : IDisposable
{
   private const string DotNetEnvironmentVariableName = "DOTNET_ENVIRONMENT";
   private readonly CancellationTokenSource cancelTokenSource = new();
   private string dotnetEnvironmentName = string.Empty;

   /// <summary>
   /// Main program.
   /// </summary>
   /// <param name="args">The command-line arguments.</param>
   private static void Main(string[] args)
   {
      // services start with the current directory set to System32; appsettings files are read
      // relative to the current directory, so point it at the executable's directory first
      Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

      new Program().Run(args);
   }

   private void Run(string[] args)
   {
      // 0 = success; 1 = failure, which also lets Windows Service recovery options restart the
      // service. WindowsBackgroundService sets Environment.ExitCode if the worker fails.
      int exitCode = 0;

      ParserResult<CommandLineOptions> result = Parser.Default.ParseArguments<CommandLineOptions>(args)
      .WithParsed(cmdLineOptions =>
      {
         // so we can use for dependency injections
         this.commandLineOptions = cmdLineOptions;

         // init logger almost first so other startup stuff can use it;
         // the initial bootstrap logger is able to log errors during start-up;
         // it is replaced by the logger configured by AddSerilog()
         Log.Logger = new LoggerConfiguration()
             .WriteTo.Console()
             .CreateBootstrapLogger();

         try
         {
            // initialize and notify user of use
            this.InitializeEnvironment();

            LogStarting();

            // build host first so we can hope to have a logger if issues come up
            using IHost host = this.CreateHost(args);

            //
            // run the service!
            //
            host.RunAsync(this.cancelTokenSource.Token).GetAwaiter().GetResult();

            exitCode = Environment.ExitCode;
         }
         catch (Exception ex)
         {
            exitCode = 1;

            // logger never initialized successfully
            if (Log.Logger.GetType().Name == "SilentLogger")
            {
               Console.ForegroundColor = ConsoleColor.Red;
               Console.Error.WriteLine($"Top-level application exception caught: {ex}");
               Console.ResetColor();
            }
            else
            {
               Log.Fatal(ex, "Top-level application exception caught");
            }
         }
      })
      .WithNotParsed(errors =>
      {
         // the parser has already written the errors (or --help/--version text) to the console
         exitCode = errors.IsHelp() || errors.IsVersion() ? 0 : 1;
      });

      // final user notifications
      LogStopping();

      // all done...close logger
      CloseLogger();

      Environment.Exit(exitCode);
   }

   private static void LogStarting()
   {
      Log.Information("Starting {Application} by {Company}, v{Version}", FilePathHelper.ApplicationName, FilePathHelper.CompanyName, Assembly.GetExecutingAssembly().GetName().Version);

      if (Environment.UserInteractive)
      {
         Log.Information("Press Ctrl-C to cancel");
      }
   }

   private static void LogStopping()
   {
      Log.Information("Stopping {Application}", FilePathHelper.ApplicationName);
   }

   private static void CloseLogger()
   {
      // serilog flush
      Log.CloseAndFlush();

      // just in case...
      if (Environment.UserInteractive)
      {
         Console.Out.Flush();
      }
   }

   private void InitializeEnvironment()
   {
      this.InitializeDotNetEnvironment();
      Log.Information("{Environment} environment detected", this.dotnetEnvironmentName.ToUpper());

      // allow ctrl-c in case running in console mode
      this.ConfigureCtrlCHandler();
   }

   /// <summary>
   /// Determines the .NET environment name: DOTNET_ENVIRONMENT if set (its appsettings file must
   /// exist), otherwise "development" if appsettings.development.json exists, otherwise
   /// "production" if appsettings.production.json exists. Debug builds copy only the development
   /// file and Release builds only the production file, so the build type picks the environment.
   /// </summary>
   private void InitializeDotNetEnvironment()
   {
      // find out if the standard .net env variable exists; if not, try to determine
      // it by the existence of an appsettings file; play it safe and default the
      // dev environment over production
      string? envName = Environment.GetEnvironmentVariable(DotNetEnvironmentVariableName);
      if (!string.IsNullOrWhiteSpace(envName))
      {
         // now be sure we have the proper appsettings file for this env
         string envFilePath = $@".\appsettings.{envName!}.json";
         if (File.Exists(envFilePath))
         {
            // we're good to allow everything else to handle the standard dotnet env
            this.dotnetEnvironmentName = envName!;
            return;
         }

         throw new ApplicationException($"{DotNetEnvironmentVariableName} environment is {envName!}, but file {envFilePath} is missing");
      }

      // try to determine if we're in development env
      string appsettingsPath = $@".\appsettings.development.json";
      if (File.Exists(appsettingsPath))
      {
         // create env variable for this process
         this.dotnetEnvironmentName = "development";
         Environment.SetEnvironmentVariable(DotNetEnvironmentVariableName, this.dotnetEnvironmentName);
         return;
      }

      // try to determine if we're in production env
      appsettingsPath = $@".\appsettings.production.json";
      if (File.Exists(appsettingsPath))
      {
         // create env variable for this process
         this.dotnetEnvironmentName = "production";
         Environment.SetEnvironmentVariable(DotNetEnvironmentVariableName, this.dotnetEnvironmentName);
         return;
      }

      // no dotnet env variable exists and no development or production appsettings exist;
      // we need something so this is, well, bad
      throw new ApplicationException($"Unable to determine DOTNET environment; no environment variable, no appsettings.{{environment}}.json");
   }

   private void ConfigureCtrlCHandler()
   {
      // allow cancellable Ctrl-C if interactive
      if (Environment.UserInteractive)
      {
         CtrlCHelper.ConfigureCtrlCHandler((sender, e) =>
         {
            // if ctrl-c pressed
            if (!e.Cancel && e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
               this.cancelTokenSource.Cancel();
               e.Cancel = true;
            }
         });
      }
   }
}