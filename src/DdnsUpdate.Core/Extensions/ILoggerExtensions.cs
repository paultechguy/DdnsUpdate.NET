// -------------------------------------------------------------------------
// <copyright file="ILoggerExtensions.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
// -------------------------------------------------------------------------


namespace DdnsUpdate.Core.Extensions;

using Microsoft.Extensions.Logging;

public static class ILoggerExtensions
{
   private const string DefaultLogName = "DdnsUpdate";

   public static void LogAny(this ILogger logger, LogLevel level, string message, string sender = DefaultLogName)
   {
      string text = $"[{sender}] {message}";
      logger.Log(level, text);
   }
}