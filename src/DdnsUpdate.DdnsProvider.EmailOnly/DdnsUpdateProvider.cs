// "// <copyright file=\"DdnsUpdateProvider.cs\" company=\"PaulTechGuy\">
// // Copyright (c) Paul Carver. All rights reserved.
// // </copyright>"

namespace DdnsUpdate.DdnsProvider.EmailOnly;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DdnsUpdate.DdnsProvider.Interfaces;
using DdnsUpdate.DdnsProvider.Models;

public class DdnsUpdateProvider(
   DdnsUpdateProviderInstanceContext contextInstance) : IDdnsUpdateProvider
{
   private bool disposedValue;
   private readonly DdnsUpdateProviderInstanceContext contextInstance = contextInstance;

   // Interface required
   /// <inheritdoc/>
   public string ProviderName { get; set; } = "PaulTechGuy.EmailOnly";

   // Interface required
   /// <inheritdoc/>
   public async Task<List<string>> GetDomainNamesAsync()
   {
      return await Task.FromResult(new List<string>());
   }

   /// <inheritdoc/>
   public async Task<DdnsProviderStatusResult> IsDomainValidAsync(string domainName)
   {
      return await Task.FromResult(DdnsProviderStatusResult.Success);
   }

   public async Task BeginBatchUpdateIpAddressAsync()
   {
      await Task.CompletedTask;
   }

   public async Task EndBatchUpdateIpAddressAsync()
   {
      await Task.CompletedTask;
   }

   // Interface required
   /// <inheritdoc/>
   public async Task<DdnsProviderStatusResult> TryUpdateIpAddressAsync(
      string domainName,
      string ipAddress)
   {
      return await Task.FromResult(DdnsProviderStatusResult.Success);
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
   // ~DdnsUpdateProvider()
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
