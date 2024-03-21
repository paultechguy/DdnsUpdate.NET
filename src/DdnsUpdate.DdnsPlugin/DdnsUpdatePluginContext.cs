// <copyright file="DdnsUpdatePluginContext.cs" company="PaulTechGuy"
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>"

namespace DdnsUpdate.DdnsPlugin;

using DdnsUpdate.DdnsPlugin.Interfaces;
using DdnsUpdate.Email.Core.Interfaces;
using Microsoft.Extensions.Configuration;

public class DdnsUpdatePluginContext(
   SettingsContext settings,
   EmailContext emailContext,
   LoggerContext loggerContext)
{
   public readonly SettingsContext Settings = settings;
   public readonly EmailContext Email = emailContext;
   public readonly LoggerContext Logger = loggerContext;

}

public class EmailContext(
   IEmailSender emailSender)
{
   /// <summary>
   /// An general email sender. Wrap calls to send email in try/catch to avoid
   /// a plugin failing.
   /// </summary>
   public readonly IEmailSender EmailSender = emailSender;
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

   /// <summary>
   /// Gets the plugin settings from the appSettings file.  See the README.ms file for details.
   /// </summary>
   /// <typeparam name="T">The class type for the plugin settings.</typeparam>
   /// <param name="plugin"><see cref="IDdnsUpdatePlugin"/>.</param>
   /// <returns></returns>
   public T? GetPluginSettingsOrNull<T>(IDdnsUpdatePlugin plugin)
      where T : class, new()
   {
      // the plugin settings should be configured in the appSettings like:
      //   "plugins": [
      //       {
      //          "name": {pluginName},
      //          "settings" {
      //             {properties for type T}
      //          }
      //       }
      //       ...{more plugins}
      //   ]

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
