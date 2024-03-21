// <copyright file="DdnsUpdatePlugin.cs" company="PaulTechGuy"
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>"

namespace DdnsUpdate.DdnsPlugin.EmailOnly;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DdnsUpdate.DdnsPlugin.Interfaces;
using DdnsUpdate.DdnsPlugin.Models;

public class DdnsUpdatePlugin(
   DdnsUpdatePluginContext context) : IDdnsUpdatePlugin
{
   private bool disposedValue;
   private readonly DdnsUpdatePluginContext context = context;

   // Interface required
   /// <inheritdoc/>
   public string PluginName { get; set; } = "PaulTechGuy.EmailOnly";

   // Interface required
   /// <inheritdoc/>
   public async Task<List<string>> GetDomainNamesAsync()
   {
      return await Task.FromResult(new List<string>());
   }

   // Interface required
   /// <inheritdoc/>
   public async Task BeginBatchUpdateIpAddressAsync()
   {
      await Task.CompletedTask;
   }

   // Interface required
   /// <inheritdoc/>
   public async Task EndBatchUpdateIpAddressAsync()
   {
      await Task.CompletedTask;
   }

   // Interface required
   /// <inheritdoc/>
   public async Task<DdnsPluginStatusResult> TryUpdateIpAddressAsync(
      string domainName,
      string ipAddress)
   {
      return await Task.FromResult(DdnsPluginStatusResult.Success);
   }

   protected virtual void Dispose(bool disposing)
   {
      if (!this.disposedValue)
      {
         if (disposing)
         {
            // dispose managed state (managed objects)
         }

         // free unmanaged resources (unmanaged objects) and override finalizer
         // set large fields to null
         this.disposedValue = true;
      }
   }

   // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
   // ~DdnsUpdatePlugin()
   // {
   //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
   //     Dispose(disposing: false);
   // }

   public void Dispose()
   {
      // Dd not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      this.Dispose(disposing: true);
      GC.SuppressFinalize(this);
   }
}
