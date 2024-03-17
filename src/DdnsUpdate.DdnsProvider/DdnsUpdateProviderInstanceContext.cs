// "// <copyright file=\"DdnsUpdateProviderInstanceContext.cs\" company=\"PaulTechGuy\">
// // Copyright (c) Paul Carver. All rights reserved.
// // </copyright>"


namespace DdnsUpdate.DdnsProvider;

using Microsoft.Extensions.Configuration;

public class DdnsUpdateProviderInstanceContext(
   IConfiguration configuration,
   LoggerContext loggerContext)
{
   public readonly IConfiguration Configuration = configuration;
   public readonly LoggerContext Logger = loggerContext;
}

public class LoggerContext(
   Action<string, string> logInformation,
   Action<string, string> logWarning,
   Action<string, string> logError,
   Action<string, string> logCritical)
{
   public readonly Action<string, string> LogInformation = logInformation;
   public readonly Action<string, string> LogWarning = logWarning;
   public readonly Action<string, string> LogError = logError;
   public readonly Action<string, string> LogCritical = logCritical;
}
