// -------------------------------------------------------------------------
// <copyright file="FilePathHelper.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
// -------------------------------------------------------------------------

namespace DdnsUpdate.DdnsProvider.Helpers;

using System;

public static class FilePathHelper
{
   // Note: Because the publish is a bundled assembly, the
   // Assembly.GetExecutingAssembly().Location returns an empty string.
   // Code in this file instead uses AppDomain.CurrentDomain.BaseDirectory

   public const string CompanyName = "PaulTechGuy";

   public const string ApplicationName = "DdnsUpdate";

   private static string configDirectory = string.Empty;

   private static string logDirectory = string.Empty;

   private static string dataDirectory = string.Empty;

   private static string pluginDirectory = string.Empty;

   public static string ApplicationPluginDirectory
   {
      get
      {
         if (string.IsNullOrEmpty(pluginDirectory))
         {
            if (OperatingSystem.IsLinux())
            {
               if (!EnvironmentHelper.IsLinuxDaemon)
               {
                  // interactive, local to the app itself
                  //   ./plugins
                  pluginDirectory = Path.Combine(
                     AppDomain.CurrentDomain.BaseDirectory,
                     "plugins");
               }
               else
               {
                  // for daemons
                  //   /etc/ddnspdate/plugins
                  pluginDirectory = Path.Combine(
                     "/etc",
                     ApplicationName.ToLower(),
                     "plugins");
               }
            }
            else if (OperatingSystem.IsWindows())
            {
               if (Environment.UserInteractive)
               {
                  // interactive, local to the app itself
                  //   .\plugins
                  pluginDirectory = Path.Combine(
                     AppDomain.CurrentDomain.BaseDirectory,
                     "plugins");
               }
               else
               {
                  // Windows service in program_data
                  //   \ProgramData\ddnsupdate\config
                  pluginDirectory = Path.Combine(
                     Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                     ApplicationName.ToLower(),
                     "plugins");
               }
            }
            else
            {
               throw new InvalidOperationException($"Invalid {nameof(OperatingSystem)} for config; not supported");
            }
         }

         return pluginDirectory;
      }
   }

   public static string ApplicationConfigDirectory
   {
      get
      {
         if (string.IsNullOrEmpty(configDirectory))
         {
            if (OperatingSystem.IsLinux())
            {
               if (!EnvironmentHelper.IsLinuxDaemon)
               {
                  // interactive, local to the app itself
                  //   ./config
                  configDirectory = Path.Combine(
                     AppDomain.CurrentDomain.BaseDirectory,
                     "config");
               }
               else
               {
                  // for daemons
                  //   /etc/ddnspdate/config
                  configDirectory = Path.Combine(
                     "/etc",
                     ApplicationName.ToLower(),
                     "config");
               }
            }
            else if (OperatingSystem.IsWindows())
            {
               if (Environment.UserInteractive)
               {
                  // interactive, local to the app itself
                  //   .\config
                  configDirectory = Path.Combine(
                     AppDomain.CurrentDomain.BaseDirectory,
                     "config");
               }
               else
               {
                  // Windows service in program_data
                  //   \ProgramData\ddnsupdate\config
                  configDirectory = Path.Combine(
                     Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                     ApplicationName.ToLower(),
                     "config");
               }
            }
            else
            {
               throw new InvalidOperationException($"Invalid {nameof(OperatingSystem)} for config; not supported");
            }
         }

         return configDirectory;
      }
   }

   public static string ApplicationDataDirectory
   {
      get
      {
         if (string.IsNullOrEmpty(dataDirectory))
         {
            if (OperatingSystem.IsLinux())
            {
               if (!EnvironmentHelper.IsLinuxDaemon)
               {
                  // interactive, local to the app itself
                  //   ./data
                  dataDirectory = Path.Combine(
                     AppDomain.CurrentDomain.BaseDirectory,
                     "data");
               }
               else
               {
                  // for daemons
                  //   /var/lib/ddnsupdate/data
                  dataDirectory = Path.Combine(
                     "/var/lib",
                     ApplicationName.ToLower(),
                     "data");
               }
            }
            else if (OperatingSystem.IsWindows())
            {
               if (Environment.UserInteractive)
               {
                  // interactive, local to the app itself
                  //   .\data
                  dataDirectory = Path.Combine(
                     AppDomain.CurrentDomain.BaseDirectory,
                     "data");
               }
               else
               {
                  // Windows service in program_data
                  //   \ProgramData\ddnsupdate\data
                  dataDirectory = Path.Combine(
                     Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                     ApplicationName.ToLower(),
                     "data");
               }
            }
            else
            {
               throw new InvalidOperationException($"Invalid {nameof(OperatingSystem)} for data; not supported");
            }
         }

         return dataDirectory;
      }
   }

   public static string ApplicationLogDirectory
   {
      get
      {
         if (string.IsNullOrEmpty(logDirectory))
         {
            if (OperatingSystem.IsLinux())
            {
               if (!EnvironmentHelper.IsLinuxDaemon)
               {
                  // interactive, local to the app itself
                  //   ./logs
                  logDirectory = Path.Combine(
                     AppDomain.CurrentDomain.BaseDirectory,
                     "logs");
               }
               else
               {
                  // for daemons
                  //   /var/logs/ddnsupdate
                  logDirectory = Path.Combine(
                     "/var/log",
                     ApplicationName.ToLower(),
                     "logs");
               }
            }
            else if (OperatingSystem.IsWindows())
            {
               if (Environment.UserInteractive)
               {
                  // interactive, local to the app itself
                  //   .\logs
                  logDirectory = Path.Combine(
                     AppDomain.CurrentDomain.BaseDirectory,
                     "logs");
               }
               else
               {
                  // Windows service in program_data
                  //   \ProgramData\ddnsupdate\logs
                  logDirectory = Path.Combine(
                     Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                     ApplicationName.ToLower(),
                     "logs");
               }
            }
            else
            {
               throw new InvalidOperationException($"Invalid {nameof(OperatingSystem)} for logs; not supported");
            }
         }

         return logDirectory;
      }
   }
}