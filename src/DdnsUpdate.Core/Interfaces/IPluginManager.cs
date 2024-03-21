// <copyright file="IPluginManager.cs" company="PaulTechGuy"
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>"

namespace DdnsUpdate.Core.Interfaces;

using DdnsUpdate.DdnsPlugin;
using DdnsUpdate.DdnsPlugin.Interfaces;
using Microsoft.Extensions.Configuration;

public interface IPluginManager
{
   void ClearPlugins();

   IList<IDdnsUpdatePlugin> Plugins { get; }

   int PluginCount { get; }

   string[] PluginNames { get; }

   int AddPlugins(
      IConfiguration configuration,
      LoggerContext loggerContext,
      string directoryPath,
      bool recursive);
}
