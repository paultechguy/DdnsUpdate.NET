// <copyright file="EmailSmtpSettings.cs" company="PaulTechGuy"
// Copyright (c) Paul Carver. All rights reserved.
// </copyright>"

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
