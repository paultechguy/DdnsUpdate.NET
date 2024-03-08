// -------------------------------------------------------------------------
// <copyright file="EmailServerSettings.cs" company="PaulTechGuy">
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>
// Use of this source code is governed by Apache License 2.0 that can
// be found at https://www.apache.org/licenses/LICENSE-2.0.
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