// -------------------------------------------------------------------------
// <copyright file="WorkerServiceCollectionExtensions.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Service;

using DdnsUpdate.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Registers the DDNS update worker.
/// </summary>
public static class WorkerServiceCollectionExtensions
{
   /// <summary>
   /// Registers <see cref="WorkerService"/> as the <see cref="IWorkerService"/>, its HTTP client
   /// for the IP address providers, the file-based <see cref="IDdnsStateStore"/>, and the system
   /// <see cref="TimeProvider"/>. The host must also
   /// configure the <c>ApplicationSettings</c> options and register an <c>IEmailSender</c> and
   /// an <c>IDdnsUpdateProvider</c>.
   /// </summary>
   /// <param name="services">The service collection.</param>
   /// <returns>The <paramref name="services"/> for chaining.</returns>
   public static IServiceCollection AddDdnsUpdateWorker(this IServiceCollection services)
   {
      // providers are tried in turn, so fail fast on a slow one instead of the 100 second default
      _ = services.AddHttpClient(WorkerService.IpAddressHttpClientName, client =>
      {
         client.Timeout = TimeSpan.FromSeconds(15);
      });

      services.TryAddSingleton(TimeProvider.System);
      services.TryAddSingleton<IDdnsStateStore, FileDdnsStateStore>();
      _ = services.AddTransient<IWorkerService, WorkerService>();

      return services;
   }
}