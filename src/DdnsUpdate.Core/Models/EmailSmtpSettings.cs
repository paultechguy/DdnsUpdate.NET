// -------------------------------------------------------------------------
// <copyright file="EmailServerSettings.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by an MIT-style license that can
// be found in the LICENSE file or at https://opensource.org/licenses/MIT.
// -------------------------------------------------------------------------

namespace DdnsUpdate.Core.Models;

public class EmailSmtpSettings
{
   public const string ConfigurationName = "EmailSmtpSettings";

   // email server

   public string? SmtpHost { get; set; }

   public int SmtpPort { get; set; }

   public bool SmtpEnableSsl { get; set; }

   public string? SmtpUsername { get; set; }

   public string? SmtpPassword { get; set; }
}