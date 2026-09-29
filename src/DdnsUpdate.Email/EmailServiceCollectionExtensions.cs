// -------------------------------------------------------------------------
// <copyright file="EmailServiceCollectionExtensions.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Email;

using DdnsUpdate.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the SMTP email sender.
/// </summary>
public static class EmailServiceCollectionExtensions
{
   /// <summary>
   /// Registers <see cref="EmailSender"/> as the <see cref="IEmailSender"/>. The SMTP settings
   /// come from the <c>ApplicationSettings</c> options, which the host must configure.
   /// </summary>
   /// <param name="services">The service collection.</param>
   /// <returns>The <paramref name="services"/> for chaining.</returns>
   public static IServiceCollection AddSmtpEmailSender(this IServiceCollection services)
   {
      _ = services.AddTransient<IEmailSender, EmailSender>();

      return services;
   }
}