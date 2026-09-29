// -------------------------------------------------------------------------
// <copyright file="FilePathHelper.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Helpers;

using System;

/// <summary>
/// Application identity and the location of its runtime data.
/// </summary>
public static class FilePathHelper
{
   /// <summary>
   /// The company name, used in the data directory path and startup message.
   /// </summary>
   public const string CompanyName = "PaulTechGuy";

   /// <summary>
   /// The application name, used in the data directory path and log messages.
   /// </summary>
   public const string ApplicationName = "DdnsUpdate";

   /// <summary>
   /// Gets the directory for logs, <c>LastIpAddress.txt</c> and <c>UriStatistics.json</c>:
   /// <c>%ProgramData%\PaulTechGuy\DdnsUpdate</c>.
   /// </summary>
   /// <remarks>
   /// ProgramData (not the executable directory or a user profile) is used so the same data is
   /// shared whether the app runs as a service account, a scheduled task, or interactively.
   /// </remarks>
   public static string ApplicationDataDirectory => Path.Combine(
         Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
         CompanyName,
         ApplicationName);
}