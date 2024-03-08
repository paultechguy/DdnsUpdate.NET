// -------------------------------------------------------------------------
// <copyright file="ApplicationSettings.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Models;
public class ApplicationSettings
{
   public const string ConfigurationName = "ApplicationSettings";

   public EmailSmtpSettings EmailSmtpSettings { get; set; }

   public WorkerServiceSettings WorkerServiceSettings { get; set; }

   public DdnsSettings DdnsSettings { get; set; }

   public ApplicationSettings()
   {
      this.EmailSmtpSettings = new EmailSmtpSettings();
      this.WorkerServiceSettings = new WorkerServiceSettings();
      this.DdnsSettings = new DdnsSettings();
   }
}