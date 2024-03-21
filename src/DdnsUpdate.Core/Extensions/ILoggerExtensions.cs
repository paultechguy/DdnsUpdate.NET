// "// <copyright file="ILoggerExtensions.cs\" company="PaulTechGuy"
// // Copyright (c) Paul Carver. All rights reserved.
// // </copyright>"

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
