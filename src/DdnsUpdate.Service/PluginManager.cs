// -------------------------------------------------------------------------
// <copyright file="PluginManager.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Service;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using DdnsUpdate.Core.Interfaces;
using DdnsUpdate.DdnsProvider;
using DdnsUpdate.DdnsProvider.Interfaces;

public class PluginManager : IPluginManager
{
   private readonly List<IDdnsUpdateProvider> providers = [];

   public IList<IDdnsUpdateProvider> Providers => this.providers.AsReadOnly();

   public void ClearProviders()
   {
      this.providers.Clear();
   }

   public int ProviderCount => this.providers.Count;

   public string[] ProviderNames => this.providers
      .Select(x => x.ProviderName)
      .ToArray();

   public int AddProviders(DdnsUpdateProviderInstanceContext context, string directoryPath, bool recursive)
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

            foreach (Type type in pluginAssembly.GetExportedTypes())
            {
               if (typeof(IDdnsUpdateProvider).IsAssignableFrom(type) && type != typeof(IDdnsUpdateProvider))
               {
                  // does this implementation have the correct ctor parameter
                  bool hasConstructorWithCorrectType = type.GetConstructors()
                     .Any(ctor => ctor.GetParameters().Any(param => param.ParameterType == typeof(DdnsUpdateProviderInstanceContext)));
                  if (!hasConstructorWithCorrectType)
                  {
                     throw new InvalidOperationException($"Plugin does not have ctor with {nameof(DdnsUpdateProviderInstanceContext)} type; {pluginPath}");
                  }

                  // create plugin instance with a ctor having the context parameter
                  var plugin = (IDdnsUpdateProvider)Activator.CreateInstance(type, context)!;
                  if (plugin is not null)
                  {
                     plugin.ProviderName = this.AdjustPluginName(plugin.ProviderName);
                     this.providers.Add(plugin);

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

   private string AdjustPluginName(string pluginName)
   {
      // make sure this pluginName is unique from any existing ones
      int index = 0;
      while (this.providers.Where(x => x.ProviderName.Equals(pluginName, StringComparison.OrdinalIgnoreCase)).Any())
      {
         pluginName = $"{pluginName}_{index}";
      }

      return pluginName;
   }
}