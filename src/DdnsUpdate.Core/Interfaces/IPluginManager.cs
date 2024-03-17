// "// <copyright file=\"IPluginManager.cs\" company=\"PaulTechGuy\">
// // Copyright (c) Paul Carver. All rights reserved.
// // </copyright>"

namespace DdnsUpdate.Core.Interfaces;

using DdnsUpdate.DdnsProvider;
using DdnsUpdate.DdnsProvider.Interfaces;

public interface IPluginManager
{
   void ClearProviders();

   IList<IDdnsUpdateProvider> Providers { get; }

   int ProviderCount { get; }

   string[] ProviderNames { get; }

   int AddProviders(DdnsUpdateProviderInstanceContext context, string directoryPath, bool recursive);
}
