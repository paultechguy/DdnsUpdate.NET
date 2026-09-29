// -------------------------------------------------------------------------
// <copyright file="ApplicationSettings.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Models;

/// <summary>
/// The provider-independent settings, bound from the <c>applicationSettings</c> section.
/// Provider settings (e.g. <c>cloudflareSettings</c>) live in their own top-level sections.
/// </summary>
public class ApplicationSettings
{
   /// <summary>
   /// The name of this configuration section.
   /// </summary>
   public const string ConfigurationName = "ApplicationSettings";

   /// <summary>
   /// Gets or sets the SMTP server used for notification email.
   /// </summary>
   public EmailSmtpSettings EmailSmtpSettings { get; set; }

   /// <summary>
   /// Gets or sets the notification email options (enabled, To, From, Reply-To).
   /// </summary>
   public WorkerServiceSettings WorkerServiceSettings { get; set; }

   /// <summary>
   /// Gets or sets the DDNS update loop settings.
   /// </summary>
   public DdnsSettings DdnsSettings { get; set; }

   /// <summary>
   /// Initializes a new instance of the <see cref="ApplicationSettings"/> class.
   /// </summary>
   public ApplicationSettings()
   {
      this.EmailSmtpSettings = new EmailSmtpSettings();
      this.WorkerServiceSettings = new WorkerServiceSettings();
      this.DdnsSettings = new DdnsSettings();
   }
}