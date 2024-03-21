// <copyright file="Program.cs" company="PaulTechGuy"
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>"

namespace DdnsUpdate.Application;

using System;
using System.Reflection;
using System.Threading;
using CommandLine;
using DdnsUpdate.Application.Helpers;
using DdnsUpdate.Core.Models;
using DdnsUpdate.DdnsPlugin.Helpers;
using Microsoft.Extensions.Hosting;
using Serilog;

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
      // first things first...need to set content root
      Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

      new Program().Run(args);
   }

   private void Run(string[] args)
   {
      ParserResult<CommandLineOptions> result = Parser.Default.ParseArguments<CommandLineOptions>(args)
      .WithParsed(cmdLineOptions =>
      {
         // so we can use for dependency injections
         this.commandLineOptions = cmdLineOptions;

         CreateBootstrapLogger();

         try
         {
            // intialize and notify user of use
            this.InitializeEnvironment();

            LogStarting();
            // build host first so we can hope to have a logger if issues come up
            IHost host = this.CreateHostBuilder().Build();

            //
            // run the service!
            //
            host.RunAsync(this.cancelTokenSource.Token).Wait();
         }
         catch (Exception ex)
         {
            string message = $"Top-level application exception caught: {ex}";

            // logger never initialized successfully
            if (Log.Logger.GetType().Name == "SilentLogger")
            {
               Console.ForegroundColor = ConsoleColor.Red;
               Console.Error.WriteLine(message);
               Console.ResetColor();
            }
            else
            {
               Log.Information(message);
            }
         }
      })
      .WithNotParsed(errors => // errors is a sequence of type IEnumerable<Error>
      {
      });

      // final user notifications
      LogStopping();

      // all done...close logger
      CloseLogger();

      // Terminate this process and return an exit code to the operating system.
      // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
      // performs one of two scenarios:
      // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
      // 2. When set to "StopHost": will cleanly stop the host, and log errors.
      //
      // In order for the Windows Service Management system to leverage configured
      // recovery options, we need to terminate the process with a non-zero exit code.

      Environment.Exit(1);
   }

   private static void CreateBootstrapLogger()
   {
      // init logger almost first so other startup stuff can use it;
      // the initial bootstrap logger is able to log errors during start-up;
      // it is replaced by the logger configured in `UseSerilog()`
      string logDirectory = FilePathHelper.ApplicationLogDirectory;
      if (!Directory.Exists(logDirectory))
      {
         Directory.CreateDirectory(logDirectory);
      }

      Log.Logger = new LoggerConfiguration()
          .WriteTo.Console()
          .CreateBootstrapLogger();
   }

   private static void LogStarting()
   {
      Log.Information($"Starting {FilePathHelper.ApplicationName}.NET by {FilePathHelper.CompanyName}, v{Assembly.GetExecutingAssembly().GetName().Version!.ToString(3)}");

      if (Environment.UserInteractive)
      {
         Log.Information("{msg}", $"Press Ctrl-C to cancel");
      }
   }

   private static void LogStopping()
   {
      Log.Information($"Stopping {FilePathHelper.ApplicationName}");
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
      Log.Information("{environment}", $"{this.dotnetEnvironmentName.ToUpper()} environment detected");

      // allow ctrl-c in case running in console mode
      this.ConfigureCtrlCHandler();
   }

   private void InitializeDotNetEnvironment()
   {
      // find out if the standard .net env variable exists; if not, try to determine
      // it by the existance of an appsetting file; play it safe and default the
      // dev environment over production
      string? envName = Environment.GetEnvironmentVariable(DotNetEnvironmentVariableName);
      if (!string.IsNullOrWhiteSpace(envName))
      {
         // now be sure we have the proper appsettings file for this env
         string envFilePath = Path.Combine(FilePathHelper.ApplicationConfigDirectory, $"appsettings.{envName!}.json");
         if (File.Exists(envFilePath))
         {
            // we're good to allow everything else to handle the standard dotnet env
            this.dotnetEnvironmentName = envName!;
            return;
         }

         throw new ApplicationException($"{DotNetEnvironmentVariableName} environment is {envName!}, but file {envFilePath} is missing");
      }

      // try to determine if we're in development env
      string appsettingsPath = Path.Combine(FilePathHelper.ApplicationConfigDirectory, "appsettings.development.json");
      if (File.Exists(appsettingsPath))
      {
         // create env variable for this process
         this.dotnetEnvironmentName = "development";
         Environment.SetEnvironmentVariable(DotNetEnvironmentVariableName, this.dotnetEnvironmentName);
         return;
      }

      // try to determine if we're in production env
      appsettingsPath = Path.Combine(FilePathHelper.ApplicationConfigDirectory, "appsettings.production.json");
      if (File.Exists(appsettingsPath))
      {
         // create env variable for this process
         this.dotnetEnvironmentName = "production";
         Environment.SetEnvironmentVariable(DotNetEnvironmentVariableName, this.dotnetEnvironmentName);
         return;
      }

      // no dotnet env variable exists and no development or production appsettings exist;
      // we need something so this is, well, bad
      throw new ApplicationException($"Unable to determine DOTNET environment; no environment variable, no appsettings.{{enviroment}}.json.  {nameof(FilePathHelper.ApplicationConfigDirectory)}: {FilePathHelper.ApplicationConfigDirectory}");
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
