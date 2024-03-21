// "// <copyright file="DdnsUpdatePluginInstanceContext.cs\" company="PaulTechGuy"
// // Copyright (c) Paul Carver. All rights reserved.
// // </copyright>"


namespace DdnsUpdate.DdnsPlugin;

using DdnsUpdate.DdnsPlugin.Interfaces;
using Microsoft.Extensions.Configuration;

public class DdnsUpdatePluginInstanceContext(
   SettingsContext settings,
   LoggerContext loggerContext)
{
   public readonly SettingsContext Settings = settings;
   public readonly LoggerContext Logger = loggerContext;
}

public class LoggerContext(
   Action<IDdnsUpdatePlugin, string> logInformation,
   Action<IDdnsUpdatePlugin, string> logWarning,
   Action<IDdnsUpdatePlugin, string> logError,
   Action<IDdnsUpdatePlugin, string> logCritical)
{
   public readonly Action<IDdnsUpdatePlugin, string> LogInformation = logInformation;
   public readonly Action<IDdnsUpdatePlugin, string> LogWarning = logWarning;
   public readonly Action<IDdnsUpdatePlugin, string> LogError = logError;
   public readonly Action<IDdnsUpdatePlugin, string> LogCritical = logCritical;
}

public class SettingsContext(
   IConfiguration configuration)
{
   private readonly IConfiguration configuration = configuration;

   public T? GetSettingsOrNull<T>(IDdnsUpdatePlugin plugin)
      where T : class, new()
   {
      // get the main "plugins" section and check children for "name" equal to the plugin name
      IConfigurationSection? pluginSection = this.configuration.GetSection("plugins");
      IEnumerable<IConfigurationSection> plugins = pluginSection.GetChildren();
      IConfigurationSection? pluginTarget = plugins.Where(p => p["name"] == plugin.PluginName).FirstOrDefault();

      // if no plugins or children with the plugin name...we're outta here
      if (pluginTarget == null)
      {
         return null;
      }

      // get the "settings" section and see if we have anything in it; if not, we're done
      IConfigurationSection settingsSection = pluginTarget.GetSection("settings");
      if (settingsSection == null || !settingsSection.GetChildren().Any())
      {
         return null;
      }

      // looks like we have "plugins" / "name": pluginName / "settings"
      var settings = new T();
      settingsSection.Bind(settings);

      return settings;
   }
}
