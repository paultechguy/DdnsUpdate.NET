// <copyright file="PluginManager.cs" company="PaulTechGuy"
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>

namespace DdnsUpdate.Service;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using DdnsUpdate.Core.Interfaces;
using DdnsUpdate.DdnsPlugin;
using DdnsUpdate.DdnsPlugin.Interfaces;
using DdnsUpdate.Email.Core.Interfaces;
using Microsoft.Extensions.Configuration;

public class PluginManager(
   IConfiguration configuration,
   IEmailSender emailSender) : IPluginManager
{
   private readonly IConfiguration configuration = configuration;
   private readonly List<IDdnsUpdatePlugin> plugins = [];
   private readonly Regex regexValidPluginName = new(@"^[\w\-\.]+$");

   public IList<IDdnsUpdatePlugin> Plugins => this.plugins.AsReadOnly();

   public void ClearPlugins()
   {
      this.plugins.Clear();
   }

   public int PluginCount => this.plugins.Count;

   public string[] PluginNames => this.plugins
      .Select(x => x.PluginName)
      .ToArray();

   public int AddPlugins(
      LoggerContext loggerContext,
      string directoryPath,
      bool recursive)
   {
      int addedCount = 0;
      foreach (string pluginPath in Directory.GetFiles(
         directoryPath,
         "*.dll",
         recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
      {
         try
         {
            Assembly pluginAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(pluginPath);

            // is this plugin disabled by using # prefix
            if (Path.GetFileName(Path.GetDirectoryName(pluginPath)!).TrimStart().StartsWith('#'))
            {
               continue; // skip this plugin directory
            }

            foreach (Type type in pluginAssembly.GetExportedTypes())
            {
               if (typeof(IDdnsUpdatePlugin).IsAssignableFrom(type) && type != typeof(IDdnsUpdatePlugin))
               {
                  // does this implementation have the correct ctor parameter
                  bool hasConstructorWithCorrectType = type.GetConstructors()
                     .Any(ctor => ctor.GetParameters().Any(param => param.ParameterType == typeof(DdnsUpdatePluginContext)));
                  if (!hasConstructorWithCorrectType)
                  {
                     throw new InvalidOperationException($"Plugin does not have ctor with {nameof(DdnsUpdatePluginContext)} type; {pluginPath}");
                  }

                  // create a context for this plugin
                  var context = new DdnsUpdatePluginContext(
                     new SettingsContext(this.configuration),
                     new EmailContext(emailSender),
                     loggerContext);

                  // create plugin instance with a ctor having the context parameter
                  var plugin = (IDdnsUpdatePlugin)Activator.CreateInstance(type, context)!;
                  if (plugin is not null)
                  {
                     if (this.plugins.Any(x => x.PluginName == plugin.PluginName))
                     {
                        throw new InvalidOperationException($"Duplicate {nameof(plugin.PluginName)} found: {plugin.PluginName}");
                     }

                     if (!this.IsPluginConfigurationValid(plugin, out IList<string> errors))
                     {
                        throw new InvalidOperationException($"Plugin failed validation: {string.Join("; ", errors)}");
                     }

                     // finally..add this pup
                     this.plugins.Add(plugin);

                     ++addedCount;
                  }
               }
            }
         }
         catch (Exception)
         {
            throw;
         }
      }

      return addedCount;
   }

   private bool IsPluginConfigurationValid(IDdnsUpdatePlugin plugin, out IList<string> errors)
   {
      errors = new List<string>();
      do
      {
         if (!this.regexValidPluginName.IsMatch(plugin.PluginName))
         {
            errors.Add($"Invalid character(s) in {nameof(plugin.PluginName)}");
         }
      }
      while (false);

      return errors.Count == 0;
   }
}
