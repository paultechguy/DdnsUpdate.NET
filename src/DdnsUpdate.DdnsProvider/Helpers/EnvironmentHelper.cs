// -------------------------------------------------------------------------
// <copyright file="EnvironmentHelper.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
// -------------------------------------------------------------------------

namespace DdnsUpdate.DdnsProvider.Helpers;

using System;

public static class EnvironmentHelper
{
   private static bool? isLinuxDaemon = null;

   public static bool IsLinuxDaemon
   {
      get
      {
         // Note: Linux daemon processes don't seem to return Environment.UserInteractive so
         // we'll use a check for an env variable that only daemon processes have.

         if (!OperatingSystem.IsLinux())
         {
            return false;
         }

         if (isLinuxDaemon is not null)
         {
            return isLinuxDaemon.Value;
         }

         // to figure out if this is running as a daemon, look for env variable that daemons have
         string? envValue = Environment.GetEnvironmentVariable("INVOCATION_ID");
         isLinuxDaemon = envValue is not null;

         return isLinuxDaemon.Value;
      }
   }
}